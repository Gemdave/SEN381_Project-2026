using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Abstractions;

public sealed record RequestOwnership(Guid RequestId, Guid? OwnerId, RequestStatus Status);

public interface IRequestRepository
{
    /// <summary>
    /// The conditional update from ADR-002. Updates the row only while it is
    /// unowned and still Received, and returns the number of rows affected.
    /// Zero means somebody else got there first. The caller must check it.
    /// </summary>
    Task<int> TryAssignAsync(Guid requestId, Guid staffUserId, DateTimeOffset now, CancellationToken ct);

    /// <summary>Guarded status change. Zero rows means the status moved underneath us.</summary>
    Task<int> TryTransitionAsync(Guid requestId, RequestStatus from, RequestStatus to, CancellationToken ct);

    Task<RequestOwnership?> GetOwnershipAsync(Guid requestId, CancellationToken ct);

    Task AddAsync(Request request, CancellationToken ct);
}
