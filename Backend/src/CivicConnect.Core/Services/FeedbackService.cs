using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;

namespace CivicConnect.Core.Services;

// FR-007. The requester pulls their feedback; nothing is pushed.
public class FeedbackService(IUnitOfWorkFactory factory, AuthorisationPolicy policy)
{
    public async Task<IReadOnlyList<FeedbackItem>> ListAsync(CurrentUser user, CancellationToken ct = default)
    {
        policy.Require(user, Permissions.ViewOwn);
        await using var uow = await factory.BeginAsync(ct);
        return await uow.Feedback.ListForRecipientAsync(user.Id, ct);
    }

    public async Task MarkReadAsync(CurrentUser user, long feedbackId, CancellationToken ct = default)
    {
        policy.Require(user, Permissions.ViewOwn);
        await using var uow = await factory.BeginAsync(ct);

        // Scoped to the recipient, so nobody can mark someone else's message.
        var found = await uow.Feedback.MarkReadAsync(feedbackId, user.Id, ct);
        if (!found)
            throw new NotFoundException("We could not find that message.");

        await uow.CommitAsync(ct);
    }
}
