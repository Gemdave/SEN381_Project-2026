using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Security;

namespace CivicConnect.Web.Endpoints;

public static class QueueEndpoints
{
    public static void MapQueueEndpoints(this WebApplication app)
    {
        // FR-008, FR-009: the staff queue with filter, sort and paging.
        app.MapGet("/api/v1/queue", async (
            IQueueRepository queue,
            ICurrentUser currentUser,
            string? status,
            Guid? categoryId,
            string? q,
            bool? mine,
            string? sort,
            int? page,
            CancellationToken ct) =>
        {
            if (!await queue.IsServiceStaffAsync(currentUser.Id, ct))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            RequestStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<RequestStatus>(status, true, out var parsedStatus))
                {
                    return Results.BadRequest(new { code = "unknown_status", message = "That status does not exist." });
                }

                statusFilter = parsedStatus;
            }

            var sortOrder = QueueSort.Oldest;
            if (!string.IsNullOrWhiteSpace(sort) && !Enum.TryParse(sort, true, out sortOrder))
            {
                return Results.BadRequest(new { code = "unknown_sort", message = "That sort order does not exist." });
            }

            var filter = new QueueFilter(statusFilter, categoryId, q, mine ?? false, sortOrder, page ?? 1);
            return Results.Ok(await queue.SearchAsync(currentUser.Id, filter, ct));
        });
    }
}
