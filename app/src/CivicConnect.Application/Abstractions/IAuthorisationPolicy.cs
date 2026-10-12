namespace CivicConnect.Application.Abstractions;

public enum RequestAction
{
    View,
    Assign,
    Transition,
    AddNote,
    Close
}

/// <summary>
/// The single authorisation decision point (ADR-005). Anything not explicitly
/// permitted is refused, so a forgotten rule fails closed.
/// </summary>
public interface IAuthorisationPolicy
{
    Task<bool> MayAsync(Guid userId, RequestAction action, Guid requestId, CancellationToken ct);

    /// <summary>FR-001. Submitting has no request yet, so it is decided on the user alone.</summary>
    Task<bool> MaySubmitAsync(Guid userId, CancellationToken ct);
}
