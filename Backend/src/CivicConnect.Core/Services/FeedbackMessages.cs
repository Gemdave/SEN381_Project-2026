using CivicConnect.Core.Models;

namespace CivicConnect.Core.Services;

// What the requester is told when a request moves (FR-007).
internal static class FeedbackMessages
{
    public static (string kind, string message, string? reason) For(
        ServiceRequest request, RequestStatus newStatus, string? reason)
    {
        var r = request.Reference;
        return newStatus switch
        {
            RequestStatus.Assigned => (FeedbackKinds.Accepted,
                $"Your request {r} has been accepted and assigned to a staff member.", null),
            RequestStatus.InProgress => (FeedbackKinds.Updated,
                $"Work has started on your request {r}.", null),
            RequestStatus.Resolved => (FeedbackKinds.Completed,
                $"Your request {r} has been resolved.", null),
            RequestStatus.Closed => (FeedbackKinds.Updated,
                $"Your request {r} has been closed.", null),
            RequestStatus.Rejected => (FeedbackKinds.Rejected,
                $"Your request {r} could not be accepted. Reason: {reason}", reason),
            _ => (FeedbackKinds.Updated, $"Your request {r} was updated.", null)
        };
    }
}
