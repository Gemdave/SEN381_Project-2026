namespace CivicConnect.Application.Abstractions;

public enum RequestAction
{
    View,
    Assign,
    Transition,
    AddNote,
    Close,

    /// <summary>CR-009 (AC-007.2): staff turn down a request that has just been received.</summary>
    Reject,

    /// <summary>FR-006 (AC-006.1): add supporting information to a request.</summary>
    Attach
}

/// <summary>
/// The single authorisation decision point (ADR-005). Anything not explicitly
/// permitted is refused, so a forgotten rule fails closed.
/// </summary>
public interface IAuthorisationPolicy
{
    Task<bool> MayAsync(Guid userId, RequestAction action, Guid requestId, CancellationToken ct);

    /// <summary>
    /// FR-001. Submitting has no request yet, so it is decided on the user alone.
    /// Refused unless the implementation says otherwise, so a policy that does not
    /// answer it fails closed.
    /// </summary>
    Task<bool> MaySubmitAsync(Guid userId, CancellationToken ct) => Task.FromResult(false);

    /// <summary>FR-015 and FR-017: managers and administrators. Refused by default.</summary>
    Task<bool> MayViewOversightAsync(Guid userId, CancellationToken ct) => Task.FromResult(false);

    /// <summary>FR-019 to FR-022: administrators only. Refused by default.</summary>
    Task<bool> MayAdministerAsync(Guid userId, CancellationToken ct) => Task.FromResult(false);

    /// <summary>FR-023: sponsors, managers and administrators. Refused by default.</summary>
    Task<bool> MayViewSummaryAsync(Guid userId, CancellationToken ct) => Task.FromResult(false);
}
