namespace CivicConnect.Domain.Requests;

/// <summary>
/// Append only accountability record (NFR-007, AC-013.1). Nothing in the
/// application updates or deletes these rows; a correction is a new entry.
/// </summary>
public class RequestHistoryEntry
{
    private RequestHistoryEntry() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequestId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static RequestHistoryEntry ForAssignment(Guid requestId, Guid actorUserId, DateTimeOffset now)
        => new()
        {
            RequestId = requestId,
            ActorUserId = actorUserId,
            Action = "Assigned",
            OldValue = null,
            NewValue = actorUserId.ToString(),
            OccurredAtUtc = now
        };

    public static RequestHistoryEntry ForTransition(
        Guid requestId, Guid actorUserId, RequestStatus from, RequestStatus to, DateTimeOffset now)
        => new()
        {
            RequestId = requestId,
            ActorUserId = actorUserId,
            Action = "StatusChanged",
            OldValue = from.ToString(),
            NewValue = to.ToString(),
            OccurredAtUtc = now
        };

    public static RequestHistoryEntry ForNote(Guid requestId, Guid actorUserId, string note, DateTimeOffset now)
        => new()
        {
            RequestId = requestId,
            ActorUserId = actorUserId,
            Action = "NoteAdded",
            Note = note,
            OccurredAtUtc = now
        };
}
