namespace CivicConnect.Domain.Access;

/// <summary>
/// ADR-005 rules that sit beside the role rules in AuthorisationPolicy, kept
/// as pure functions so they are tested without a database.
/// </summary>
public static class RequestAccess
{
    /// <summary>FR-001. Requests are submitted by requesters, not on their behalf.</summary>
    public static bool MaySubmit(UserRole role) => role == UserRole.Requester;

    /// <summary>
    /// FR-008 (AC-008.1, AC-008.2). Staff act only on requests in their scoped
    /// categories. Other roles are not limited by category; their own rules apply.
    /// </summary>
    public static bool WithinScope(UserRole role, bool inScope) => role != UserRole.ServiceStaff || inScope;
}
