namespace CivicConnect.Domain.Access;

/// <summary>
/// ADR-005 rules as pure functions, so the decision table is tested without a
/// database. inScope means the request's category is one of the staff member's
/// scoped categories (FR-008).
/// </summary>
public static class RequestAccess
{
    /// <summary>FR-001. Requests are submitted by requesters, not on their behalf.</summary>
    public static bool MaySubmit(UserRole role) => role == UserRole.Requester;

    /// <summary>AC-004.2 and NFR-003 for requesters, AC-008.1 and AC-008.2 for staff.</summary>
    public static bool MayView(UserRole role, Guid userId, Guid requesterId, bool inScope) => role switch
    {
        UserRole.Requester => requesterId == userId,
        UserRole.ServiceStaff => inScope,
        UserRole.Management or UserRole.Administrator => true,
        _ => false
    };

    /// <summary>AC-011.1. Only staff may accept, and only inside their scope.</summary>
    public static bool MayAssign(UserRole role, bool inScope)
        => role == UserRole.ServiceStaff && inScope;

    /// <summary>AC-012.1 and AC-014.2. Only the owning staff member, inside their scope.</summary>
    public static bool MayWorkOn(UserRole role, Guid userId, Guid? ownerId, bool inScope)
        => role == UserRole.ServiceStaff && inScope && ownerId == userId;
}
