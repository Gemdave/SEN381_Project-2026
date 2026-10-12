using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Security;

namespace CivicConnect.Web.Endpoints;

/// <summary>
/// ADR-006. Submitting (FR-001) and moving a request's status (FR-012, FR-014).
/// Validation failures are problem details with one message per field
/// (AC-001.2); a refused status move is 400 with a message (AC-012.2).
/// </summary>
public static class RequestEndpoints
{
    public sealed record StatusChange(string? Status, string? Note);

    public static void MapRequestEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/requests", async (
            NewRequest input,
            SubmissionService service,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var result = await service.SubmitAsync(currentUser.Id, input, ct);

            return result.Outcome switch
            {
                SubmissionOutcome.Created => Results.Created(
                    "/api/v1/requests/" + result.RequestId,
                    new { id = result.RequestId, reference = result.Reference, status = nameof(RequestStatus.Received) }),
                SubmissionOutcome.Invalid => Results.ValidationProblem(
                    result.Errors.ToDictionary(e => e.Key, e => new[] { e.Value })),
                _ => Results.StatusCode(StatusCodes.Status403Forbidden)
            };
        });

        app.MapPatch("/api/v1/requests/{id:guid}/status", async (
            Guid id,
            StatusChange change,
            StatusTransitionService service,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<RequestStatus>(change.Status, ignoreCase: false, out var to)
                || !Enum.IsDefined(to))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["Status must be one of: " + string.Join(", ", Enum.GetNames<RequestStatus>()) + "."]
                });
            }

            var outcome = await service.MoveAsync(id, currentUser.Id, to, change.Note, ct);

            return outcome switch
            {
                TransitionOutcome.Changed => Results.Ok(new { id, status = to.ToString() }),
                TransitionOutcome.NotAllowed => Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Status change not allowed",
                    detail: "That status change is not allowed from the request's current status."),
                TransitionOutcome.NoteTooLong => Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["note"] = [$"Please keep the note to {StatusTransitionService.NoteMax} characters or fewer."]
                }),
                TransitionOutcome.Conflicted => Results.Conflict(new
                {
                    code = "status_changed",
                    message = "The status changed while you were working. Reload and try again."
                }),
                TransitionOutcome.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
                _ => Results.NotFound()
            };
        });
    }
}
