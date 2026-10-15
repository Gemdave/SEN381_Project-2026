using System.Net.Http.Json;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// The remaining buildable FRs through the real pages: the requester filter
/// (FR-005), the oversight view (FR-015, FR-017), category maintenance (FR-021)
/// and the audit trail (FR-022). Figures are checked against the database.
/// </summary>
[Collection(WebAppCollection.Name)]
public class ManagementScreensTests(WebAppFixture app)
{
    private const string Manager = "Dev Manager";
    private const string Administrator = "Dev Administrator";

    private sealed record Created(Guid Id, string Reference);

    private async Task<Created> SubmitFaultAsync(HttpClient requester, string title)
    {
        var response = await requester.PostAsJsonAsync("/api/v1/requests", new
        {
            title,
            description = title + ", raised by a management screen test.",
            location = "Civic centre",
            categoryId = await app.CategoryIdAsync("Fault")
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Created>())!;
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-005")]
    public async Task A_requester_filters_their_own_requests_by_status()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var created = await SubmitFaultAsync(requester, "Cracked paving");
        var listed = ">" + created.Reference + "</a>";

        Assert.Contains(listed, Html.BodyOf(await requester.GetStringAsync("/Requests?status=Received")));
        Assert.DoesNotContain(listed, Html.BodyOf(await requester.GetStringAsync("/Requests?status=Closed")));

        var other = await app.ClientForAsync(DevUsers.RequesterTwo);
        Assert.Contains("No requests match this filter.", Html.BodyOf(await other.GetStringAsync("/Requests?status=InProgress")));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-015")]
    public async Task Oversight_counts_match_the_request_records_and_follow_the_filters()
    {
        var manager = await app.ClientForAsync(Manager);
        var fault = await app.CategoryIdAsync("Fault");

        var all = await app.QueryAsync(db => db.Requests.CountAsync());
        Assert.Contains($"<span class=\"total\">{all}</span>", Html.BodyOf(await manager.GetStringAsync("/Oversight")));

        var received = await app.QueryAsync(db => db.Requests.CountAsync(r => r.Status == RequestStatus.Received));
        Assert.Contains($"<span class=\"total\">{received}</span>", Html.BodyOf(await manager.GetStringAsync("/Oversight?status=Received")));

        var faults = await app.QueryAsync(db => db.Requests.CountAsync(r => r.CategoryId == fault));
        Assert.Contains($"<span class=\"total\">{faults}</span>", Html.BodyOf(await manager.GetStringAsync("/Oversight?categoryId=" + fault)));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-015")]
    public async Task Oversight_is_for_managers_and_administrators_only()
    {
        var admin = await app.ClientForAsync(Administrator);
        var staff = await app.ClientForAsync(DevUsers.StaffOne);

        Assert.Contains("Total requests", Html.BodyOf(await admin.GetStringAsync("/Oversight")));

        var refused = Html.BodyOf(await staff.GetStringAsync("/Oversight"));
        Assert.Contains("The oversight view is for managers and administrators.", refused);
        Assert.DoesNotContain("Total requests", refused);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-021")]
    public async Task An_administrator_adds_and_retires_a_category_and_the_submit_form_follows()
    {
        var admin = await app.ClientForAsync(Administrator);
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var token = Html.Token(await admin.GetStringAsync("/Admin/Categories"));
        var name = "Parks " + Guid.NewGuid().ToString("N")[..6];
        var option = "<span>" + name + "</span>";

        var added = await Html.PostFormAsync(admin, "/Admin/Categories?handler=Add", token,
            new Dictionary<string, string> { ["name"] = name });
        Assert.Equal("Category added.", Html.Message(await added.Content.ReadAsStringAsync()));
        Assert.Contains(option, await requester.GetStringAsync("/Requests/Submit"));

        var duplicate = await Html.PostFormAsync(admin, "/Admin/Categories?handler=Add", token,
            new Dictionary<string, string> { ["name"] = name.ToUpperInvariant() });
        Assert.Equal("A category with that name already exists.", Html.Message(await duplicate.Content.ReadAsStringAsync()));

        var id = await app.CategoryIdAsync(name);
        var retired = await Html.PostFormAsync(admin, "/Admin/Categories?handler=Retire", token,
            new Dictionary<string, string> { ["id"] = id.ToString() });
        Assert.StartsWith("Category retired.", Html.Message(await retired.Content.ReadAsStringAsync()));
        Assert.DoesNotContain(option, await requester.GetStringAsync("/Requests/Submit"));

        var staff = await app.ClientForAsync(DevUsers.StaffOne);
        Assert.Contains("Category maintenance is for administrators.", Html.BodyOf(await staff.GetStringAsync("/Admin/Categories")));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-022")]
    public async Task The_audit_trail_lists_request_and_category_actions_with_names()
    {
        var admin = await app.ClientForAsync(Administrator);
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var staff = await app.ClientForAsync(DevUsers.StaffOne);

        var created = await SubmitFaultAsync(requester, "Leaning signpost");
        (await staff.PostAsync($"/api/v1/requests/{created.Id}/assignment", null)).EnsureSuccessStatusCode();

        var token = Html.Token(await admin.GetStringAsync("/Admin/Categories"));
        var name = "Audit " + Guid.NewGuid().ToString("N")[..6];
        await Html.PostFormAsync(admin, "/Admin/Categories?handler=Add", token, new Dictionary<string, string> { ["name"] = name });

        var trail = Html.BodyOf(await admin.GetStringAsync("/Admin/Audit"));
        Assert.Contains("Category added", trail);
        Assert.Contains(name, trail);
        Assert.Contains(created.Reference, trail);
        Assert.Contains("to " + DevUsers.StaffOne, trail);
        Assert.Contains("by " + DevUsers.StaffOne, trail);

        var categoriesOnly = Html.BodyOf(await admin.GetStringAsync("/Admin/Audit?kind=Categories"));
        Assert.Contains(name, categoriesOnly);
        Assert.DoesNotContain(created.Reference, categoriesOnly);

        var manager = await app.ClientForAsync(Manager);
        Assert.Contains("The audit trail is for administrators.", Html.BodyOf(await manager.GetStringAsync("/Admin/Audit")));
    }
}
