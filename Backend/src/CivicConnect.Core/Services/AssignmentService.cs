using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;

namespace CivicConnect.Core.Services;

// FR-011 / NFR-005 / ADR-002.
// The claim is one conditional UPDATE. If two staff press "accept" together,
// the database lets one through and the other gets zero rows back.
public class AssignmentService(IUnitOfWorkFactory factory, AuthorisationPolicy policy)
{
    private const string TakenMessage = "Someone else has already taken this request.";

    public async Task<ServiceRequest> AcceptAsync(CurrentUser user, Guid requestId, CancellationToken ct = default)
    {
        policy.Require(user, Permissions.Assign);

        await using var uow = await factory.BeginAsync(ct);

        var request = await uow.Requests.FindAsync(requestId, ct)
            ?? throw new NotFoundException("We could not find that request.");

        // Quick answer for the common case. The UPDATE below is what actually protects us.
        if (request.AssignedTo is not null)
            throw new ConflictException(TakenMessage);

        var claimed = await uow.Requests.TryAssignAsync(requestId, user.Id, ct);
        if (!claimed)
            throw new ConflictException(TakenMessage);

        await uow.History.AddAsync(requestId, "Assigned", request.Status, RequestStatus.Assigned, user.Id, null, ct);

        var (kind, message, reason) = FeedbackMessages.For(request, RequestStatus.Assigned, null);
        await uow.Feedback.AddAsync(requestId, request.RequesterId, kind, message, reason, ct);

        var updated = await uow.Requests.FindAsync(requestId, ct)
            ?? throw new NotFoundException("We could not find that request.");

        await uow.CommitAsync(ct);
        return updated;
    }
}
