using CivicConnect.Core.Models;

namespace CivicConnect.Core.Rules;

// The one place that answers "may this person do this?" (NFR-006).
// Full role management is Gerald's part (ADR-005); this covers what Build Slice 1 needs.
public class AuthorisationPolicy
{
    public void Require(CurrentUser user, string permission)
    {
        if (!user.Permissions.Contains(permission))
            throw new ForbiddenException("You are not allowed to do that.");
    }

    // Requesters see only their own requests (NFR-003).
    // Staff see requests nobody owns yet, plus the ones they own (AC-008.2).
    public bool CanSee(CurrentUser user, ServiceRequest request)
    {
        if (request.RequesterId == user.Id && user.Permissions.Contains(Permissions.ViewOwn))
            return true;

        if (user.Permissions.Contains(Permissions.ViewDetail))
            return request.AssignedTo is null || request.AssignedTo == user.Id;

        return false;
    }

    public bool CanSeeInternals(CurrentUser user) =>
        user.Permissions.Contains(Permissions.ViewDetail);
}
