namespace CivicConnect.Domain.Audit;

/// <summary>
/// FR-022 and NFR-007. An insert-only record of a controlled action that is not
/// on a request, such as a category change. Request actions stay in the request
/// history; the audit view shows both.
/// </summary>
public class AuditEntry
{
    private AuditEntry() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string? Detail { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditEntry Record(Guid actorUserId, string action, string subject, string? detail, DateTimeOffset now)
        => new()
        {
            ActorUserId = actorUserId,
            Action = action,
            Subject = subject,
            Detail = detail,
            OccurredAtUtc = now
        };
}
