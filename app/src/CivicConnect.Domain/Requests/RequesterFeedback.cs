namespace CivicConnect.Domain.Requests;

/// <summary>
/// An in-app message for the requester (FR-007). It is saved in the same
/// transaction as the change that caused it.
/// </summary>
public class RequesterFeedback
{
    private RequesterFeedback() { }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid RequestId { get; private set; }

    public Guid RequesterId { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static RequesterFeedback Create(Guid requestId, Guid requesterId, string message, DateTimeOffset now)
        => new()
        {
            RequestId = requestId,
            RequesterId = requesterId,
            Message = message,
            CreatedAtUtc = now
        };
}
