using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Requests;

public enum TransitionOutcome
{
    Changed,
    NotAllowed,
    Conflicted,
    NotFound,
    Forbidden
}

/// <summary>
/// FR-012. The allowed moves come from the domain, and the write is guarded on
/// the status we validated, so a change that lands first is never overwritten.
/// </summary>
public sealed class StatusTransitionService(
    IRequestRepository requests,
    IRequestHistoryRepository history,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<TransitionOutcome> MoveAsync(
        Guid requestId, Guid staffUserId, RequestStatus to, CancellationToken ct)
    {
        if (!await policy.MayAsync(staffUserId, RequestAction.Transition, requestId, ct))
        {
            return TransitionOutcome.Forbidden;
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var ownership = await requests.GetOwnershipAsync(requestId, token);
            if (ownership is null)
            {
                return TransitionOutcome.NotFound;
            }

            if (!StatusTransition.IsAllowed(ownership.Status, to))
            {
                return TransitionOutcome.NotAllowed;
            }

            var rows = await requests.TryTransitionAsync(requestId, ownership.Status, to, token);
            if (rows == 0)
            {
                return TransitionOutcome.Conflicted;
            }

            history.Add(RequestHistoryEntry.ForTransition(requestId, staffUserId, ownership.Status, to, clock.UtcNow));
            await unitOfWork.SaveChangesAsync(token);

            return TransitionOutcome.Changed;
        }, ct);
    }
}
