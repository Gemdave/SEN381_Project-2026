using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Requests;

public sealed class AssignmentService(
    IRequestRepository requests,
    IRequestHistoryRepository history,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock,
    IFeedbackRepository? feedback = null)
{
    public async Task<AssignmentResult> AcceptAsync(Guid requestId, Guid staffUserId, CancellationToken ct)
    {
        if (!await policy.MayAsync(staffUserId, RequestAction.Assign, requestId, ct))
        {
            return AssignmentResult.Forbidden();
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.UtcNow;
            var rows = await requests.TryAssignAsync(requestId, staffUserId, now, token);

            if (rows == 0)
            {
                var ownership = await requests.GetOwnershipAsync(requestId, token);
                if (ownership is null)
                {
                    return AssignmentResult.NotFound();
                }

                // Owned by someone: a clear conflict. Unowned but not open any more: say so.
                return ownership.OwnerId is { } owner
                    ? AssignmentResult.AlreadyOwned(owner)
                    : AssignmentResult.NotAvailable();
            }

            history.Add(RequestHistoryEntry.ForAssignment(requestId, staffUserId, now));

            // FR-007: tell the requester their request was accepted.
            var saved = await requests.GetOwnershipAsync(requestId, token);
            if (feedback is not null && saved is not null && saved.RequesterId != Guid.Empty)
            {
                feedback.Add(RequesterFeedback.Create(
                    requestId, saved.RequesterId, "Your request has been accepted by a staff member.", now));
            }

            await unitOfWork.SaveChangesAsync(token);
            return AssignmentResult.Assigned(staffUserId);
        }, ct);
    }
}
