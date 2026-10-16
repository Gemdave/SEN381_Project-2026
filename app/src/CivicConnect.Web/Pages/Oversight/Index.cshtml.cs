using System.Globalization;
using System.Text;
using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Oversight;

/// <summary>
/// FR-015 to FR-018. Activity counts by status and by category for managers
/// and administrators (AC-015.1). Choosing a status, category or lifecycle state
/// updates the counts and the list together (AC-016.1, AC-017.1); overdue
/// requests are flagged (AC-016.2). The service performance table and the
/// matching requests export as CSV with exactly what the page shows (AC-018.1,
/// AC-018.2, CR-006).
/// </summary>
public class IndexModel(
    IOversightQueries oversight,
    IRequestQueries queries,
    IAuthorisationPolicy policy,
    ICurrentUser currentUser) : PageModel
{
    public static readonly string[] FigureColumns =
        ["Category", "Requests", "Open", "Overdue", "Resolved", "Resolved within target", "Average hours to resolve"];

    public static readonly string[] RowColumns =
        ["Reference", "Title", "Category", "Status", "Submitted (UTC)", "Target response (UTC)", "Overdue"];

    [BindProperty(SupportsGet = true)]
    public RequestStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Lifecycle? Lifecycle { get; set; }

    public bool Allowed { get; private set; }
    public OversightSummary? Summary { get; private set; }
    public IReadOnlyList<CategoryOption> Categories { get; private set; } = [];

    public bool Filtered => Status is not null || CategoryId is not null || Lifecycle is not null;

    /// <summary>The current filters as a query string, so the export links export what is shown.</summary>
    public string FilterQuery
        => string.Join("&", new[]
        {
            Status is null ? null : "status=" + Status,
            CategoryId is null ? null : "categoryId=" + CategoryId,
            Lifecycle is null ? null : "lifecycle=" + Lifecycle
        }.Where(p => p is not null));

    private OversightFilter Filter => new(Status, CategoryId, Lifecycle);

    public async Task OnGetAsync(CancellationToken ct)
    {
        Allowed = await policy.MayViewOversightAsync(currentUser.Id, ct);
        if (!Allowed)
        {
            return;
        }

        Categories = await queries.ListActiveCategoriesAsync(ct);
        Summary = await oversight.SummaryAsync(Filter, ct);
    }

    /// <summary>AC-018.2: the performance table as displayed, one line per row, then the total.</summary>
    public async Task<IActionResult> OnGetExportFiguresAsync(CancellationToken ct)
    {
        if (!await policy.MayViewOversightAsync(currentUser.Id, ct))
        {
            return Forbid403();
        }

        var summary = await oversight.SummaryAsync(Filter, ct);
        var lines = new List<IReadOnlyList<string>> { FigureColumns };
        lines.AddRange(FigureRows(summary));
        return Csv(lines, "service-performance.csv");
    }

    /// <summary>AC-018.2: every request behind the figures, with its overdue flag.</summary>
    public async Task<IActionResult> OnGetExportRequestsAsync(CancellationToken ct)
    {
        if (!await policy.MayViewOversightAsync(currentUser.Id, ct))
        {
            return Forbid403();
        }

        var rows = await oversight.RowsAsync(Filter, ct);
        var lines = new List<IReadOnlyList<string>> { RowColumns };
        lines.AddRange(rows.Select(r => (IReadOnlyList<string>)
        [
            r.Reference,
            r.Title,
            r.Category,
            StatusText(r.Status),
            r.CreatedAtUtc.ToString("u", CultureInfo.InvariantCulture),
            r.TargetResponseAtUtc.ToString("u", CultureInfo.InvariantCulture),
            r.Overdue ? "Yes" : "No"
        ]));
        return Csv(lines, "requests.csv");
    }

    /// <summary>The performance table's rows, shared by the page and the export so they cannot differ.</summary>
    public static IEnumerable<IReadOnlyList<string>> FigureRows(OversightSummary summary)
    {
        foreach (var c in summary.ByCategory)
        {
            yield return [c.Category, Text(c.Count), Text(c.Open), Text(c.Overdue), Text(c.Resolved), Text(c.ResolvedWithinTarget), Hours(c.AverageHoursToResolve)];
        }

        yield return ["Total", Text(summary.Total), Text(summary.Open), Text(summary.Overdue), Text(summary.Resolved), Text(summary.ResolvedWithinTarget), Hours(summary.AverageHoursToResolve)];
    }

    public static string StatusText(RequestStatus status) => status == RequestStatus.InProgress ? "In progress" : status.ToString();

    public static string Hours(double? hours) => hours?.ToString("0.0", CultureInfo.InvariantCulture) ?? "None yet";

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static StatusCodeResult Forbid403() => new(StatusCodes.Status403Forbidden);

    private static FileContentResult Csv(IEnumerable<IReadOnlyList<string>> lines, string fileName)
    {
        var text = new StringBuilder();
        foreach (var line in lines)
        {
            text.Append(string.Join(",", line.Select(Cell))).Append("\r\n");
        }

        return new FileContentResult(Encoding.UTF8.GetBytes(text.ToString()), "text/csv; charset=utf-8")
        {
            FileDownloadName = fileName
        };
    }

    /// <summary>
    /// Quotes where needed, and stops a requester's title that starts with =, +,
    /// - or @ from running as a formula when the file is opened in a spreadsheet.
    /// </summary>
    private static string Cell(string value)
    {
        if (value.Length > 0 && "=+-@".Contains(value[0]) && !double.TryParse(value, CultureInfo.InvariantCulture, out _))
        {
            value = "'" + value;
        }

        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}
