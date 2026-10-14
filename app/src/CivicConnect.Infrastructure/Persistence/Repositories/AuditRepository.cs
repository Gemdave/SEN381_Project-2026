using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository(CivicConnectDbContext db) : IAuditRepository
{
    public void Add(AuditEntry entry) => db.AuditEntries.Add(entry);

    public async Task<IReadOnlyList<AuditItem>> RecentAsync(AuditKind kind, int take, CancellationToken ct)
    {
        var items = new List<AuditItem>();

        if (kind is AuditKind.All or AuditKind.Requests)
        {
            items.AddRange(await db.RequestHistory.AsNoTracking()
                .Join(db.Requests, h => h.RequestId, r => r.Id, (h, r) => new { h, r.Reference })
                .OrderByDescending(x => x.h.OccurredAtUtc)
                .Take(take)
                .Select(x => new AuditItem(
                    x.h.OccurredAtUtc, x.h.Action, x.Reference, x.h.OldValue, x.h.NewValue, x.h.Note, x.h.ActorUserId))
                .ToListAsync(ct));
        }

        if (kind is AuditKind.All or AuditKind.Categories)
        {
            items.AddRange(await db.AuditEntries.AsNoTracking()
                .OrderByDescending(a => a.OccurredAtUtc)
                .Take(take)
                .Select(a => new AuditItem(a.OccurredAtUtc, a.Action, a.Subject, null, null, a.Detail, a.ActorUserId))
                .ToListAsync(ct));
        }

        return items.OrderByDescending(i => i.OccurredAtUtc).Take(take).ToList();
    }
}
