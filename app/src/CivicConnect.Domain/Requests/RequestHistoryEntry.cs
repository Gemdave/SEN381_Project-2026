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

    /// <summary>FR-001. The first entry of every request, written with the request itself.</summary>
    public static RequestHistoryEntry ForSubmission(Guid requestId, Guid requesterId, DateTimeOffset now)
        => new()
        {
            RequestId = requestId,
            ActorUserId = requesterId,
            Action = "Submitted",
            NewValue = RequestStatus.Received.ToString(),
            OccurredAtUtc = now
        };

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

    /// <summary>
    /// FR-012. The optional note carries resolution information when a request
    /// is resolved or closed (AC-014.1).
    /// </summary>
    public static RequestHistoryEntry ForTransition(
        Guid requestId, Guid actorUserId, RequestStatus from, RequestStatus to, DateTimeOffset now, string? note = null)
        => new()
        {
            RequestId = requestId,
            ActorUserId = actorUserId,
            Action = "StatusChanged",
            OldValue = from.ToString(),
            NewValue = to.ToString(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            OccurredAtUtc = now
        };

    /// <summary>FR-006. The file name is the new value, so the history reads "Attachment added to photo.jpg".</summary>
    public static RequestHistoryEntry ForAttachment(Guid requestId, Guid actorUserId, string fileName, DateTimeOffset now)
        => new()
        {
            RequestId = requestId,
            ActorUserId = actorUserId,
            Action = "AttachmentAdded",
            NewValue = fileName,
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
