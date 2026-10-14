using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Repositories;

public sealed class QueueRepository(CivicConnectDbContext db) : IQueueRepository
{
    public Task<bool> IsServiceStaffAsync(Guid userId, CancellationToken ct)
        => db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.Role == UserRole.ServiceStaff, ct);

    public async Task<IReadOnlyList<CategoryOption>> CategoriesAsync(CancellationToken ct)
        => await db.Categories
            .AsNoTracking()
            .Where(c => !c.IsRetired)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption(c.Id, c.Name))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<QueueRow>> SearchAsync(Guid staffUserId, QueueFilter filter, CancellationToken ct)
    {
        // FR-008: staff see unassigned requests and their own, nothing else, and
        // only in the categories they are scoped to (AC-008.1, AC-008.2).
        var requests = db.Requests
            .AsNoTracking()
            .Where(r => r.AssignedToUserId == null || r.AssignedToUserId == staffUserId)
            .Where(r => db.StaffCategoryScopes.Any(s => s.UserId == staffUserId && s.CategoryId == r.CategoryId));

        // FR-009: filter. A rejected request needs no more work (CR-009), so it
        // leaves the queue unless staff ask for rejected requests by name.
        if (filter.Status is { } status)
        {
            requests = requests.Where(r => r.Status == status);
        }
        else
        {
            requests = requests.Where(r => r.Status != RequestStatus.Rejected);
        }

        if (filter.CategoryId is { } categoryId)
        {
            requests = requests.Where(r => r.CategoryId == categoryId);
        }

        if (filter.MineOnly)
        {
            requests = requests.Where(r => r.AssignedToUserId == staffUserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            requests = requests.Where(r => r.Title.ToLower().Contains(term) || r.Reference.ToLower().Contains(term));
        }

        var joined = requests.Join(
            db.Categories,
            r => r.CategoryId,
            c => c.Id,
            (r, c) => new { Request = r, Category = c.Name });

        // FR-009: sort. The reference is always the tie-breaker so the order never wobbles.
        // Sorting happens before the QueueRow is built, because EF cannot sort on a record's properties.
        var ordered = filter.Sort switch
        {
            QueueSort.Newest => joined.OrderByDescending(x => x.Request.CreatedAtUtc).ThenBy(x => x.Request.Reference),
            QueueSort.Reference => joined.OrderBy(x => x.Request.Reference),
            QueueSort.Status => joined.OrderBy(x => x.Request.Status).ThenBy(x => x.Request.CreatedAtUtc).ThenBy(x => x.Request.Reference),
            _ => joined.OrderBy(x => x.Request.CreatedAtUtc).ThenBy(x => x.Request.Reference)
        };

        // ASR-04: always paged.
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var page = Math.Max(filter.Page, 1);

        return await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QueueRow(
                x.Request.Id,
                x.Request.Reference,
                x.Request.Title,
                x.Request.Status,
                x.Category,
                x.Request.CreatedAtUtc,
                x.Request.AssignedToUserId != null))
            .ToListAsync(ct);
    }
}
