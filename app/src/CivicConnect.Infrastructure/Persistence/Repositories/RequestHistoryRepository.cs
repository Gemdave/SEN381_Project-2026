using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Repositories;

public sealed class RequestHistoryRepository(CivicConnectDbContext db) : IRequestHistoryRepository
{
    public void Add(RequestHistoryEntry entry) => db.RequestHistory.Add(entry);

    public async Task<IReadOnlyList<RequestHistoryEntry>> ForRequestAsync(Guid requestId, CancellationToken ct)
        => await db.RequestHistory
            .Where(h => h.RequestId == requestId)
            .OrderBy(h => h.OccurredAtUtc)
            .ToListAsync(ct);
}
