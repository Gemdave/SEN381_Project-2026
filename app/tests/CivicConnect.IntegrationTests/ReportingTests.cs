using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using CivicConnect.Domain.Requests;
using CivicConnect.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// FR-016, FR-018 and FR-023 through the real pages, with every figure checked
/// against the request records (NFR-008): open and overdue requests, the
/// service performance table and its exports, and the sponsor summary.
/// </summary>
[Collection(WebAppCollection.Name)]
public partial class ReportingTests(WebAppFixture app)
{
    private const string Manager = "Dev Manager";
    private const string Administrator = "Dev Administrator";

    private sealed record Created(Guid Id, string Reference);

    private Task<int> OpenCountAsync()
        => app.QueryAsync(db => db.Requests.CountAsync(r =>
            r.Status == RequestStatus.Received || r.Status == RequestStatus.Assigned || r.Status == RequestStatus.InProgress));

    private Task<int> OverdueCountAsync()
    {
        var now = DateTimeOffset.UtcNow;
        return app.QueryAsync(db => db.Requests.CountAsync(r =>
            (r.Status == RequestStatus.Received || r.Status == RequestStatus.Assigned || r.Status == RequestStatus.InProgress)
            && r.TargetResponseAtUtc < now));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-016.2")]
    public async Task A_request_past_its_target_is_flagged_and_counted_as_overdue()
    {
        var manager = await app.ClientForAsync(Manager);

        var page = Html.BodyOf(await manager.GetStringAsync("/Oversight?lifecycle=Overdue"));

        Assert.Contains($"<span class=\"total\">{await OverdueCountAsync()}</span>", page);
        var seeded = page.IndexOf(">" + DevelopmentSeed.OverdueReference + "</a>", StringComparison.Ordinal);
        Assert.True(seeded >= 0, "The seeded overdue request is listed.");
        Assert.Contains("<span class=\"overdue\">Overdue</span>", page[seeded..]);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-016.1")]
    public async Task Open_resolved_and_closed_requests_are_each_identifiable()
    {
        var manager = await app.ClientForAsync(Manager);

        Assert.Contains($"<span class=\"total\">{await OpenCountAsync()}</span>", Html.BodyOf(await manager.GetStringAsync("/Oversight?lifecycle=Open")));

        foreach (var status in new[] { RequestStatus.Resolved, RequestStatus.Closed, RequestStatus.Rejected })
        {
            var count = await app.QueryAsync(db => db.Requests.CountAsync(r => r.Status == status));
            Assert.Contains($"<span class=\"total\">{count}</span>", Html.BodyOf(await manager.GetStringAsync("/Oversight?status=" + status)));
        }
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "NFR-008")]
    public async Task Every_figure_reconciles_with_the_request_records()
    {
        var manager = await app.ClientForAsync(Manager);
        var page = Html.BodyOf(await manager.GetStringAsync("/Oversight"));
        var table = TableRows(page);

        var total = await app.QueryAsync(db => db.Requests.CountAsync());
        Assert.Contains($"<span class=\"total\">{total}</span>", page);
        Assert.Contains($"<span class=\"open\">{await OpenCountAsync()}</span>", page);

        // The by-category rows add up to the total row, and the total row to the records.
        var totalRow = table[^1];
        Assert.Equal("Total", totalRow[0]);
        Assert.Equal(total, int.Parse(totalRow[1]));
        for (var column = 1; column <= 5; column++)
        {
            Assert.Equal(int.Parse(totalRow[column]), table[..^1].Sum(row => int.Parse(row[column])));
        }

        var faultCount = await app.QueryAsync(db =>
            db.Requests.Join(db.Categories, r => r.CategoryId, c => c.Id, (r, c) => c.Name).CountAsync(n => n == "Fault"));
        Assert.Equal(faultCount.ToString(), table.Single(row => row[0] == "Fault")[1]);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-018.2")]
    public async Task The_exports_match_what_is_displayed()
    {
        var manager = await app.ClientForAsync(Manager);
        var fault = await app.CategoryIdAsync("Fault");
        var query = "categoryId=" + fault;

        var shown = TableRows(Html.BodyOf(await manager.GetStringAsync("/Oversight?" + query)));
        var figures = await manager.GetAsync("/Oversight?handler=ExportFigures&" + query);
        Assert.Equal("text/csv", figures.Content.Headers.ContentType?.MediaType);
        var exported = CsvRows(await figures.Content.ReadAsStringAsync());

        Assert.Equal(CivicConnect.Web.Pages.Oversight.IndexModel.FigureColumns, exported[0]);
        Assert.Equal(shown.Count, exported.Count - 1);
        for (var i = 0; i < shown.Count; i++)
        {
            Assert.Equal(shown[i], exported[i + 1]);
        }

        var rows = CsvRows(await manager.GetStringAsync("/Oversight?handler=ExportRequests&" + query))[1..];
        var references = await app.QueryAsync(db => db.Requests.Where(r => r.CategoryId == fault).Select(r => r.Reference).ToListAsync());
        Assert.Equal(references.Order(), rows.Select(r => r[0]).Order());
        Assert.Equal(shown[^1][1], rows.Count.ToString());
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-018.2")]
    public async Task Only_managers_and_administrators_can_export()
    {
        var staff = await app.ClientForAsync(DevUsers.StaffOne);

        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/Oversight?handler=ExportFigures")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/Oversight?handler=ExportRequests")).StatusCode);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-023.1")]
    public async Task A_sponsor_sees_the_summary_and_no_individual_requests()
    {
        var sponsor = await app.ClientForAsync(DevelopmentSeed.Sponsor);

        var summary = Html.BodyOf(await sponsor.GetStringAsync("/Reports/Summary"));
        Assert.Contains($"<dd class=\"total\">{await app.QueryAsync(db => db.Requests.CountAsync())}</dd>", summary);
        Assert.Contains($"<dd class=\"overdue-count\">{await OverdueCountAsync()}</dd>", summary);
        Assert.DoesNotContain("REQ-", summary);

        Assert.Contains("The oversight view is for managers and administrators.", Html.BodyOf(await sponsor.GetStringAsync("/Oversight")));
        var anyRequest = await app.QueryAsync(db => db.Requests.Select(r => r.Id).FirstAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await sponsor.GetAsync("/Staff/Details?id=" + anyRequest)).StatusCode);

        var staff = await app.ClientForAsync(DevUsers.StaffOne);
        Assert.Contains("The service summary is for sponsors, managers and administrators.", Html.BodyOf(await staff.GetStringAsync("/Reports/Summary")));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "CR-005")]
    public async Task A_category_target_applies_to_requests_submitted_after_it_changes()
    {
        var admin = await app.ClientForAsync(Administrator);
        var token = Html.Token(await admin.GetStringAsync("/Admin/Categories"));
        var name = "Urgent " + Guid.NewGuid().ToString("N")[..6];
        await Html.PostFormAsync(admin, "/Admin/Categories?handler=Add", token, new Dictionary<string, string> { ["name"] = name });
        var category = await app.CategoryIdAsync(name);

        var set = await Html.PostFormAsync(admin, "/Admin/Categories?handler=Target", token,
            new Dictionary<string, string> { ["id"] = category.ToString(), ["hours"] = "4" });
        Assert.StartsWith("Target changed.", Html.Message(await set.Content.ReadAsStringAsync()));

        var refused = await Html.PostFormAsync(admin, "/Admin/Categories?handler=Target", token,
            new Dictionary<string, string> { ["id"] = category.ToString(), ["hours"] = "0" });
        Assert.StartsWith("A target must be between", Html.Message(await refused.Content.ReadAsStringAsync()));

        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var response = await requester.PostAsJsonAsync("/api/v1/requests", new
        {
            title = "Water leak",
            description = "Water is coming through the ceiling.",
            location = "Library",
            categoryId = category
        });
        var created = (await response.Content.ReadFromJsonAsync<Created>())!;

        var stored = await app.QueryAsync(db => db.Requests.Where(r => r.Id == created.Id)
            .Select(r => new { r.CreatedAtUtc, r.TargetResponseAtUtc }).SingleAsync());
        Assert.Equal(TimeSpan.FromHours(4), stored.TargetResponseAtUtc - stored.CreatedAtUtc);
    }

    /// <summary>The performance table's data rows (the header row has no td cells).</summary>
    private static List<string[]> TableRows(string page)
    {
        var table = Html.Match(page, PerformanceTable());
        return RowPattern().Matches(table)
            .Select(row => CellPattern().Matches(row.Value).Select(c => WebUtility.HtmlDecode(c.Groups[1].Value)).ToArray())
            .Where(cells => cells.Length > 0)
            .ToList();
    }

    /// <summary>Enough CSV for these files: quoted cells may hold commas and doubled quotes.</summary>
    private static List<string[]> CsvRows(string csv)
        => csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(line => CsvCell().Matches(line)
                .Select(m => m.Groups[1].Success ? m.Groups[1].Value.Replace("\"\"", "\"") : m.Groups[2].Value)
                .ToArray())
            .ToList();

    [GeneratedRegex("<table class=\"performance\">(.*?)</table>", RegexOptions.Singleline)]
    private static partial Regex PerformanceTable();

    [GeneratedRegex("<tr>(.*?)</tr>", RegexOptions.Singleline)]
    private static partial Regex RowPattern();

    [GeneratedRegex("<td>(.*?)</td>", RegexOptions.Singleline)]
    private static partial Regex CellPattern();

    [GeneratedRegex("(?:^|,)(?:\"((?:[^\"]|\"\")*)\"|([^,]*))")]
    private static partial Regex CsvCell();
}
