using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Repositories;

public sealed class RequestRepository(CivicConnectDbContext db) : IRequestRepository
{
    /// <summary>
    /// ADR-002. One statement: the database evaluates the precondition and
    /// applies the write atomically. Two staff accepting at the same moment
    /// produce one row affected and one zero, never two owners.
    /// </summary>
    public Task<int> TryAssignAsync(Guid requestId, Guid staffUserId, DateTimeOffset now, CancellationToken ct)
        => db.Requests
            .Where(r => r.Id == requestId
                        && r.AssignedToUserId == null
                        && r.Status == RequestStatus.Received)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.AssignedToUserId, staffUserId)
                .SetProperty(r => r.AssignedAtUtc, now)
                .SetProperty(r => r.Status, RequestStatus.Assigned), ct);

    public Task<int> TryTransitionAsync(Guid requestId, RequestStatus from, RequestStatus to, CancellationToken ct)
        => db.Requests
            .Where(r => r.Id == requestId && r.Status == from)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, to), ct);

    public async Task<RequestOwnership?> GetOwnershipAsync(Guid requestId, CancellationToken ct)
        => await db.Requests
            .Where(r => r.Id == requestId)
            .Select(r => new RequestOwnership(r.Id, r.AssignedToUserId, r.Status))
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Request request, CancellationToken ct)
        => await db.Requests.AddAsync(request, ct);
}
