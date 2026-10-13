using CivicConnect.Application.Abstractions;
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
/// onto QueueFilter, the same as GET /api/v1/queue.
/// </summary>
public class QueueModel(IQueueRepository queue, ICurrentUser currentUser) : PageModel
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

    public bool Filtered => Status is not null || CategoryId is not null || !string.IsNullOrWhiteSpace(Q) || Mine;

    public async Task OnGetAsync(CancellationToken ct)
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

        Items = rows
            .Select(r => new StaffQueueItem(r.Id, r.Reference, r.Title, r.Status, r.Category, r.CreatedAtUtc, r.IsAssigned))
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
