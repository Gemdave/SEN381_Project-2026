namespace CivicConnect.Domain.Requests;

/// <summary>
/// The only place the request lifecycle is defined (ADR-002).
/// Received -> Assigned -> In Progress -> Resolved -> Closed.
/// AC-012.2 requires anything outside this map to be refused.
/// </summary>
public static class StatusTransition
{
    private static readonly Dictionary<RequestStatus, RequestStatus[]> Allowed = new()
    {
        [RequestStatus.Received] = [RequestStatus.Assigned],
        [RequestStatus.Assigned] = [RequestStatus.InProgress],
        [RequestStatus.InProgress] = [RequestStatus.Resolved],
        [RequestStatus.Resolved] = [RequestStatus.Closed],
        [RequestStatus.Closed] = []
    };

    public static bool IsAllowed(RequestStatus from, RequestStatus to)
        => Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static IReadOnlyList<RequestStatus> NextFrom(RequestStatus from)
        => Allowed.TryGetValue(from, out var next) ? next : [];
}
