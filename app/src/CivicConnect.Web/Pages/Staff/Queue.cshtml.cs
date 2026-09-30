using CivicConnect.Domain.Requests;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Web.Pages.Staff;

/// <summary>
/// FR-008. The page model is the composition point from ADR-004: it loads only
/// what this role may see, and the partials know nothing about roles.
/// </summary>
public class QueueModel(CivicConnectDbContext db) : PageModel
{
    public IReadOnlyList<StaffQueueItem> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Scope is applied in the query, so rows this user may not see are never
        // loaded (ADR-005). Team scoping arrives with the full RBAC model.
        Items = await db.Requests
            .AsNoTracking()
            .Where(r => r.Status == RequestStatus.Received || r.Status == RequestStatus.Assigned)
            .OrderBy(r => r.CreatedAtUtc)
            .Join(db.Categories, r => r.CategoryId, c => c.Id, (r, c) => new StaffQueueItem(
                r.Id,
                r.Reference,
                r.Title,
                r.Status,
                c.Name,
                r.CreatedAtUtc,
                r.AssignedToUserId != null))
            .Take(50)
            .ToListAsync(ct);
    }
}
