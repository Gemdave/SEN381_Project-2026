using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;

namespace CivicConnect.Core.Services;

// FR-013. Notes are only ever added, never edited or removed.
public class NoteService(IUnitOfWorkFactory factory, AuthorisationPolicy policy)
{
    public async Task<RequestNote> AddAsync(CurrentUser user, Guid requestId, string? body, CancellationToken ct = default)
    {
        policy.Require(user, Permissions.AddNote);

        var errors = new Dictionary<string, string[]>();
        Validation.CheckText(errors, "body", body, 2000, "Write the note before saving.");
        if (errors.Count > 0)
            throw new ValidationFailedException(errors);

        await using var uow = await factory.BeginAsync(ct);

        var request = await uow.Requests.FindAsync(requestId, ct);
        if (request is null || !policy.CanSee(user, request))
            throw new NotFoundException("We could not find that request.");

        if (request.AssignedTo != user.Id)
            throw new ForbiddenException("Only the person who owns this request can add notes.");

        var note = await uow.Notes.AddAsync(requestId, user.Id, body!.Trim(), ct);
        await uow.CommitAsync(ct);
        return note;
    }
}
