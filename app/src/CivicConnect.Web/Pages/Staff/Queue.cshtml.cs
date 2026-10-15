using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Models;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Staff;

/// <summary>
/// FR-008 and FR-009. The rows come from IQueueRepository, which applies the
/// category scope and ownership inside the query (ADR-005), so rows this user
/// may not see are never loaded. Filter, search, sort and paging map straight
/// onto QueueFilter, the same as GET /api/v1/queue. Overdue rows are marked
/// (FR-016), and a request nobody owns can be accepted from its row (FR-011),
/// through the same AssignmentService as the details page.
/// </summary>
public class QueueModel(
    IQueueRepository queue,
    AssignmentService assignment,
    IClock clock,
    ICurrentUser currentUser) : PageModel
{
    public const int PageSize = 20;

    [BindProperty(SupportsGet = true)]
    public RequestStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Mine { get; set; }

    [BindProperty(SupportsGet = true)]
    public QueueSort Sort { get; set; } = QueueSort.Oldest;

    // "p", because Razor Pages already uses "page" for routing.
    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; } = 1;

    public bool IsStaff { get; private set; }
    public IReadOnlyList<StaffQueueItem> Items { get; private set; } = [];
    public IReadOnlyList<CategoryOption> Categories { get; private set; } = [];
    public bool HasNextPage { get; private set; }
    public PageMessage? Message { get; private set; }

    public bool Filtered => Status is not null || CategoryId is not null || !string.IsNullOrWhiteSpace(Q) || Mine;

    /// <summary>The current filters, so the Accept form posts back to the same view.</summary>
    public Dictionary<string, string> RouteValues
    {
        get
        {
            var values = new Dictionary<string, string> { ["sort"] = Sort.ToString(), ["p"] = PageNumber.ToString() };
            if (Status is not null) values["status"] = Status.ToString()!;
            if (CategoryId is not null) values["categoryId"] = CategoryId.ToString()!;
            if (!string.IsNullOrWhiteSpace(Q)) values["q"] = Q;
            if (Mine) values["mine"] = "true";
            return values;
        }
    }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    /// <summary>FR-011 from the queue: the same conditional accept as the details page (AC-011.2).</summary>
    public async Task<IActionResult> OnPostAcceptAsync(Guid id, CancellationToken ct)
    {
        var result = await assignment.AcceptAsync(id, currentUser.Id, ct);

        Message = PageMessage.From(result.Outcome == AssignmentOutcome.Assigned, result.Outcome switch
        {
            AssignmentOutcome.Assigned => "You now own this request. It stays in your queue.",
            AssignmentOutcome.AlreadyOwned => "Another staff member has already accepted this request.",
            AssignmentOutcome.Forbidden => "You are not permitted to accept this request.",
            AssignmentOutcome.NotAvailable => "This request can no longer be accepted.",
            _ => "That request no longer exists."
        });

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        IsStaff = await queue.IsServiceStaffAsync(currentUser.Id, ct);
        if (!IsStaff)
        {
            return;
        }

        PageNumber = Math.Max(PageNumber, 1);
        Categories = await queue.CategoriesAsync(ct);

        var rows = await queue.SearchAsync(
            currentUser.Id, new QueueFilter(Status, CategoryId, Q, Mine, Sort, PageNumber, PageSize), ct);

        var now = clock.UtcNow;
        Items = rows
            .Select(r => new StaffQueueItem(
                r.Id, r.Reference, r.Title, r.Status, r.Category, r.CreatedAtUtc, r.IsAssigned,
                r.TargetResponseAtUtc, ResponseTarget.IsOverdue(r.Status, r.TargetResponseAtUtc, now)))
            .ToList();
        HasNextPage = rows.Count == PageSize;
    }

    /// <summary>The same view on another page, keeping every filter.</summary>
    public string? LinkTo(int pageNumber)
        => Url.Page("/Staff/Queue", new
        {
            status = Status,
            categoryId = CategoryId,
            q = Q,
            mine = Mine ? true : (bool?)null,
            sort = Sort,
            p = pageNumber
        });
}
