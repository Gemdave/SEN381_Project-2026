namespace CivicConnect.Domain.Requests;

/// <summary>
/// FR-016 (AC-016.2) and CR-005. Each category carries a target response time
/// in hours; a request takes its due time from its category when it is
/// submitted, so a later change to the target does not move existing requests.
/// A request is overdue while it is still open and its due time has passed.
/// </summary>
public static class ResponseTarget
{
    public const int DefaultHours = 72;
    public const int MinHours = 1;

    /// <summary>Ninety days. Anything longer is not a response target.</summary>
    public const int MaxHours = 2160;

    public static bool IsValidHours(int hours) => hours is >= MinHours and <= MaxHours;

    /// <summary>Open means staff still owe the requester an outcome (AC-016.1).</summary>
    public static bool IsOpen(RequestStatus status)
        => status is RequestStatus.Received or RequestStatus.Assigned or RequestStatus.InProgress;

    public static DateTimeOffset DueAt(DateTimeOffset submittedAt, int hours) => submittedAt.AddHours(hours);

    public static bool IsOverdue(RequestStatus status, DateTimeOffset dueAt, DateTimeOffset now)
        => IsOpen(status) && now > dueAt;
}
