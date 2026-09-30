using CivicConnect.Core.Models;

namespace CivicConnect.Core.Rules;

// The whole lifecycle in one small table (FR-012).
public static class StatusRules
{
    private static readonly Dictionary<RequestStatus, RequestStatus[]> Allowed = new()
    {
        [RequestStatus.Received] = [RequestStatus.Assigned, RequestStatus.Rejected],
        [RequestStatus.Assigned] = [RequestStatus.InProgress],
        [RequestStatus.InProgress] = [RequestStatus.Resolved],
        [RequestStatus.Resolved] = [RequestStatus.Closed],
        [RequestStatus.Closed] = [],
        [RequestStatus.Rejected] = []
    };

    public static bool CanMove(RequestStatus from, RequestStatus to) =>
        Allowed[from].Contains(to);

    // Plain-language text for the screens (AC-003.1).
    public static string Display(RequestStatus status) => status switch
    {
        RequestStatus.InProgress => "In Progress",
        _ => status.ToString()
    };
}
