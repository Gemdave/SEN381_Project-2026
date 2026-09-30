using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;

namespace CivicConnect.Core.Services;

// FR-012, FR-014, FR-007. The status, the history row and the requester's
// feedback are written together or not at all.
public class StatusTransitionService(IUnitOfWorkFactory factory, AuthorisationPolicy policy)
{
    public async Task<ServiceRequest> TransitionAsync(
        CurrentUser user, Guid requestId, RequestStatus next, string? reason, CancellationToken ct = default)
    {
        policy.Require(user, Permissions.UpdateStatus);
        if (next == RequestStatus.Closed)
            policy.Require(user, Permissions.Close);

        if (next == RequestStatus.Assigned)
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["status"] = ["Use the accept action to take ownership of a request."]
            });

        var cleanReason = reason?.Trim();
        if (next == RequestStatus.Rejected)
        {
            var errors = new Dictionary<string, string[]>();
            Validation.CheckText(errors, "reason", cleanReason, 500, "Say why the request is being rejected.");
            if (errors.Count > 0)
                throw new ValidationFailedException(errors);
        }

        await using var uow = await factory.BeginAsync(ct);

        var request = await uow.Requests.FindAsync(requestId, ct);
        if (request is null || !policy.CanSee(user, request))
            throw new NotFoundException("We could not find that request.");

        if (!StatusRules.CanMove(request.Status, next))
            throw new ConflictException(
                $"A request cannot move from {StatusRules.Display(request.Status)} to {StatusRules.Display(next)}.");

        // Rejecting happens before anyone owns the request; every other step belongs to the owner.
        if (next != RequestStatus.Rejected && request.AssignedTo != user.Id)
            throw new ForbiddenException("Only the person who owns this request can update it.");

        var changed = await uow.Requests.TryChangeStatusAsync(requestId, request.Status, next, ct);
        if (!changed)
            throw new ConflictException("This request was changed a moment ago. Please refresh and try again.");

        var savedReason = next == RequestStatus.Rejected ? cleanReason : null;
        await uow.History.AddAsync(requestId, "StatusChanged", request.Status, next, user.Id, savedReason, ct);

        var (kind, message, feedbackReason) = FeedbackMessages.For(request, next, savedReason);
        await uow.Feedback.AddAsync(requestId, request.RequesterId, kind, message, feedbackReason, ct);

        var updated = await uow.Requests.FindAsync(requestId, ct)
            ?? throw new NotFoundException("We could not find that request.");

        await uow.CommitAsync(ct);
        return updated;
    }
}
