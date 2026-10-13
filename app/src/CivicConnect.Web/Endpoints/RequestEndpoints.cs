using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Security;

namespace CivicConnect.Web.Endpoints;

/// <summary>
/// ADR-006. Submitting a request (FR-001). Validation failures are problem
/// details with one message per field (AC-001.2). Status changes and notes are
/// in StatusEndpoints.
/// </summary>
public static class RequestEndpoints
{
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
    }
}
