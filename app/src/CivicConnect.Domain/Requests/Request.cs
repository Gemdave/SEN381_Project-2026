namespace CivicConnect.Domain.Requests;

/// <summary>
/// A service request. Ownership and status are deliberately not settable from
/// outside: they change through the services in the Application layer, which
/// run the conditional update and write history in one transaction (ADR-002).
/// </summary>
public class Request
{
    private Request() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Reference { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public Guid RequesterId { get; private set; }
    public RequestStatus Status { get; private set; } = RequestStatus.Received;
    public Guid? AssignedToUserId { get; private set; }
    public DateTimeOffset? AssignedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>FR-001. AC-001.2 requires every missing required field to be reported.</summary>
    public static Request Submit(
        string reference,
        string title,
        string description,
        Guid categoryId,
        Guid requesterId,
        DateTimeOffset now)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(reference)) missing.Add(nameof(reference));
        if (string.IsNullOrWhiteSpace(title)) missing.Add(nameof(title));
        if (string.IsNullOrWhiteSpace(description)) missing.Add(nameof(description));
        if (categoryId == Guid.Empty) missing.Add(nameof(categoryId));
        if (requesterId == Guid.Empty) missing.Add(nameof(requesterId));

        if (missing.Count > 0)
        {
            throw new ArgumentException("Required fields are missing: " + string.Join(", ", missing));
        }

        return new Request
        {
            Reference = reference,
            Title = title,
            Description = description,
            CategoryId = categoryId,
            RequesterId = requesterId,
            Status = RequestStatus.Received,
            CreatedAtUtc = now
        };
    }
}
