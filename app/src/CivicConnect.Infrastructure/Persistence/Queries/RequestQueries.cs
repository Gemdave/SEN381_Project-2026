using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Queries;

/// <summary>
/// Read only projections for the pages (ADR-004). Nothing here authorises;
/// callers ask the policy first, or the query is scoped to the caller.
/// </summary>
public sealed class RequestQueries(CivicConnectDbContext db) : IRequestQueries
{
    public async Task<IReadOnlyList<CategoryOption>> ListActiveCategoriesAsync(CancellationToken ct)
        => await db.Categories.AsNoTracking()
            .Where(c => !c.IsRetired)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption(c.Id, c.Name))
            .ToListAsync(ct);

    public Task<bool> IsActiveCategoryAsync(Guid categoryId, CancellationToken ct)
        => db.Categories.AnyAsync(c => c.Id == categoryId && !c.IsRetired, ct);

    public async Task<int> TargetHoursAsync(Guid categoryId, CancellationToken ct)
        => await db.Categories.AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => (int?)c.TargetResponseHours)
            .FirstOrDefaultAsync(ct) ?? ResponseTarget.DefaultHours;

    public async Task<IReadOnlyList<RequestListItem>> ListForRequesterAsync(
        Guid requesterId, RequestStatus? status, CancellationToken ct)
        => await (
            from r in db.Requests.AsNoTracking()
            join c in db.Categories on r.CategoryId equals c.Id
            where r.RequesterId == requesterId && (status == null || r.Status == status)
            orderby r.CreatedAtUtc descending, r.Reference descending
            select new RequestListItem(r.Id, r.Reference, r.Title, c.Name, r.Status, r.CreatedAtUtc))
            .Take(50)
            .ToListAsync(ct);

    public Task<RequestDetail?> GetDetailAsync(Guid requestId, CancellationToken ct)
        => (
            from r in db.Requests.AsNoTracking()
            join c in db.Categories on r.CategoryId equals c.Id
            join u in db.Users on r.RequesterId equals u.Id
            where r.Id == requestId
            select new RequestDetail(
                r.Id, r.Reference, r.Title, r.Description, r.Location,
                c.Name, u.DisplayName, r.Status, r.CreatedAtUtc))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, string>> DisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        return await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
    }
}
