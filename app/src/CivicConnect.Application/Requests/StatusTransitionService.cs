using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Requests;

public enum TransitionOutcome
{
    Changed,
    NotAllowed,
    Conflicted,
    NotFound,
    Forbidden,
    NoteRequired
}

public sealed class StatusTransitionService(
    IRequestRepository requests,
    IRequestHistoryRepository history,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock,
    IFeedbackRepository feedback)
{
    public async Task<TransitionOutcome> MoveAsync(
        Guid requestId, Guid staffUserId, RequestStatus to, string? note, CancellationToken ct)
    {
        // Closing (FR-014, AC-014.2) and rejecting (CR-009) have their own permissions.
        var action = to switch
        {
            RequestStatus.Closed => RequestAction.Close,
            RequestStatus.Rejected => RequestAction.Reject,
            _ => RequestAction.Transition
        };
        if (!await policy.MayAsync(staffUserId, action, requestId, ct))
        {
            return TransitionOutcome.Forbidden;
        }

        // Resolving records how it was resolved (AC-014.1); rejecting records why (AC-007.2).
        var cleanNote = note?.Trim();
        if (to is RequestStatus.Resolved or RequestStatus.Rejected && string.IsNullOrEmpty(cleanNote))
        {
            return TransitionOutcome.NoteRequired;
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

            var now = clock.UtcNow;
            history.Add(RequestHistoryEntry.ForTransition(requestId, staffUserId, ownership.Status, to, now));
            if (!string.IsNullOrEmpty(cleanNote))
            {
                history.Add(RequestHistoryEntry.ForNote(requestId, staffUserId, cleanNote, now));
            }

            // FR-007: the requester is told about every status change.
            if (ownership.RequesterId != Guid.Empty)
            {
                feedback.Add(RequesterFeedback.Create(requestId, ownership.RequesterId, MessageFor(to, cleanNote), now));
            }

            await unitOfWork.SaveChangesAsync(token);
            return TransitionOutcome.Changed;
        }, ct);
    }

    /// <summary>
    /// CR-009 (AC-007.2). Staff in scope turn down a request that has just been
    /// received, with a reason the requester is shown.
    /// </summary>
    public Task<TransitionOutcome> RejectAsync(Guid requestId, Guid staffUserId, string? reason, CancellationToken ct)
        => MoveAsync(requestId, staffUserId, RequestStatus.Rejected, reason, ct);

    private static string MessageFor(RequestStatus to, string? note) => to switch
    {
        RequestStatus.InProgress => "Work has started on your request.",
        RequestStatus.Resolved => "Your request has been resolved: " + note,
        RequestStatus.Closed => "Your request has been closed.",
        RequestStatus.Rejected => "Your request was not accepted: " + note,
        _ => "Your request was updated."
    };
}
