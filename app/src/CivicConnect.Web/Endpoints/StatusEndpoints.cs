using CivicConnect.Application.Requests;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Security;

namespace CivicConnect.Web.Endpoints;

public sealed record StatusRequest(string Status, string? Note);

public sealed record NoteRequest(string Text);

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this WebApplication app)
    {
        // FR-012, FR-014: controlled status changes.
        app.MapPost("/api/v1/requests/{id:guid}/status", async (
            Guid id,
            StatusRequest body,
            StatusTransitionService service,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<RequestStatus>(body.Status, true, out var to))
            {
                return Results.BadRequest(new { code = "unknown_status", message = "That status does not exist." });
            }

            var outcome = await service.MoveAsync(id, currentUser.Id, to, body.Note, ct);
            return outcome switch
            {
                TransitionOutcome.Changed => Results.Ok(new { requestId = id, status = to.ToString() }),
                TransitionOutcome.NotAllowed => Results.Conflict(new
                {
                    code = "transition_not_allowed",
                    message = "That status change is not allowed from the current status."
                }),
                TransitionOutcome.Conflicted => Results.Conflict(new
                {
                    code = "status_changed",
                    message = "The request changed while you were working on it. Reload and try again."
                }),
                TransitionOutcome.NoteRequired => Results.BadRequest(new
                {
                    code = "note_required",
                    message = "Resolving a request needs a note saying how it was resolved."
                }),
                TransitionOutcome.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
                _ => Results.NotFound()
            };
        });

        // FR-013: comments on a request.
        app.MapPost("/api/v1/requests/{id:guid}/notes", async (
            Guid id,
            NoteRequest body,
            NoteService service,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var outcome = await service.AddAsync(id, currentUser.Id, body.Text, ct);
            return outcome switch
            {
                NoteOutcome.Added => Results.Ok(new { requestId = id }),
                NoteOutcome.Empty => Results.BadRequest(new { code = "note_empty", message = "A note cannot be empty." }),
                NoteOutcome.TooLong => Results.BadRequest(new { code = "note_too_long", message = "A note can be 2000 characters at most." }),
                _ => Results.StatusCode(StatusCodes.Status403Forbidden)
            };
        });
    }
}
