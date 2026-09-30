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
}
