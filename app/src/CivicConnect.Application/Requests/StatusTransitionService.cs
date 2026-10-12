using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Requests;

public enum TransitionOutcome
{
    Changed,
    NotAllowed,
    NoteTooLong,
    Conflicted,
    NotFound,
    Forbidden
}

/// <summary>
/// FR-012 and FR-014. The allowed moves come from the domain, and the write is
/// guarded on the status we validated, so a change that lands first is never
/// overwritten. Closing is checked as its own action (AC-014.2), and the note
/// carries resolution information (AC-014.1).
/// </summary>
public sealed class StatusTransitionService(
    IRequestRepository requests,
    IRequestHistoryRepository history,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const int NoteMax = 500;

    public async Task<TransitionOutcome> MoveAsync(
        Guid requestId, Guid staffUserId, RequestStatus to, string? note, CancellationToken ct)
    {
        var action = to == RequestStatus.Closed ? RequestAction.Close : RequestAction.Transition;
        if (!await policy.MayAsync(staffUserId, action, requestId, ct))
        {
            return TransitionOutcome.Forbidden;
        }

        if (note is not null && note.Trim().Length > NoteMax)
        {
            return TransitionOutcome.NoteTooLong;
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

            history.Add(RequestHistoryEntry.ForTransition(
                requestId, staffUserId, ownership.Status, to, clock.UtcNow, note));
            await unitOfWork.SaveChangesAsync(token);

            return TransitionOutcome.Changed;
        }, ct);
    }
}
