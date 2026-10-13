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
        // Closing has its own permission (FR-014, AC-014.2).
        var action = to == RequestStatus.Closed ? RequestAction.Close : RequestAction.Transition;
        if (!await policy.MayAsync(staffUserId, action, requestId, ct))
        {
            return TransitionOutcome.Forbidden;
        }

        // Resolving records how it was resolved (AC-014.1).
        var cleanNote = note?.Trim();
        if (to == RequestStatus.Resolved && string.IsNullOrEmpty(cleanNote))
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

    private static string MessageFor(RequestStatus to, string? note) => to switch
    {
        RequestStatus.InProgress => "Work has started on your request.",
        RequestStatus.Resolved => "Your request has been resolved: " + note,
        RequestStatus.Closed => "Your request has been closed.",
        _ => "Your request was updated."
    };
}
