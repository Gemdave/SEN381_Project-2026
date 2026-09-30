using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Requests;

/// <summary>
/// FR-011 and NFR-005. This is the class the M2 demonstration trace points at.
/// </summary>
public sealed class AssignmentService(
    IRequestRepository requests,
    IRequestHistoryRepository history,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock)
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

            // ADR-002: the precondition lives in the write itself, so no window
            // exists between checking and writing.
            var rows = await requests.TryAssignAsync(requestId, staffUserId, now, token);

            if (rows == 0)
            {
                var ownership = await requests.GetOwnershipAsync(requestId, token);
                if (ownership is null)
                {
                    return AssignmentResult.NotFound();
                }

                return ownership.OwnerId is { } owner
                    ? AssignmentResult.AlreadyOwned(owner)
                    : AssignmentResult.NotFound();
            }

            history.Add(RequestHistoryEntry.ForAssignment(requestId, staffUserId, now));
            await unitOfWork.SaveChangesAsync(token);

            return AssignmentResult.Assigned(staffUserId);
        }, ct);
    }
}
