namespace CivicConnect.Domain.Requests;

/// <summary>
/// The controlled status vocabulary (AC-012.1). Status is never free text,
/// which is what lets FR-003 show the same words to a requester that staff set.
/// </summary>
public enum RequestStatus
{
    Received = 0,
    Assigned = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}
