namespace CivicConnect.Application.Results;

public enum AssignmentOutcome
{
    Assigned,
    AlreadyOwned,
    NotFound,
    Forbidden
}

/// <summary>
/// Why this type exists: a zero row conditional update must never be able to
/// read as success (AC-011.2). Callers switch on the outcome rather than on a
/// boolean, and ADR-006 maps AlreadyOwned to HTTP 409.
/// </summary>
public sealed record AssignmentResult(AssignmentOutcome Outcome, Guid? OwnerId)
{
    public static AssignmentResult Assigned(Guid ownerId) => new(AssignmentOutcome.Assigned, ownerId);

    public static AssignmentResult AlreadyOwned(Guid ownerId) => new(AssignmentOutcome.AlreadyOwned, ownerId);

    public static AssignmentResult NotFound() => new(AssignmentOutcome.NotFound, null);

    public static AssignmentResult Forbidden() => new(AssignmentOutcome.Forbidden, null);
}
