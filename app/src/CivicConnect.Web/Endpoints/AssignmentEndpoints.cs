using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Web.Security;

namespace CivicConnect.Web.Endpoints;

/// <summary>
/// ADR-006. The interesting outcome is visible in the status code: 409 when
/// another staff member already owns the request (AC-011.2).
/// </summary>
public static class AssignmentEndpoints
{
    public static void MapAssignmentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/requests/{id:guid}/assignment", async (
            Guid id,
            AssignmentService service,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var result = await service.AcceptAsync(id, currentUser.Id, ct);

            return result.Outcome switch
            {
                AssignmentOutcome.Assigned => Results.Ok(new { requestId = id, ownerId = result.OwnerId }),
                AssignmentOutcome.AlreadyOwned => Results.Conflict(new
                {
                    code = "request_already_owned",
                    message = "Another staff member has already accepted this request.",
                    ownerId = result.OwnerId
                }),
                AssignmentOutcome.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
                _ => Results.NotFound()
            };
        });
    }
}
