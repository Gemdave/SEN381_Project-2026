using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;

namespace CivicConnect.Web.UI;

/// <summary>
/// ADR-004. One place builds the capabilities for every page, from the same
/// permissions and the same lifecycle table the services enforce, so the screen
/// and the server cannot disagree about what a role may do.
/// </summary>
public sealed class CapabilityFactory(AuthorisationPolicy policy)
{
    public RequestCapabilities For(CurrentUser user, ServiceRequest request)
    {
        var canAccept =
            user.Permissions.Contains(Permissions.Assign)
            && request.AssignedTo is null
            && request.Status == RequestStatus.Received;

        var owns = request.AssignedTo == user.Id;
        var canTransition = owns && user.Permissions.Contains(Permissions.UpdateStatus);

        var next = canTransition ? Allowed(user, request.Status) : [];

        return new RequestCapabilities(
            CanAccept: canAccept,
            CanTransition: canTransition && next.Count > 0,
            CanSeeInternals: policy.CanSeeInternals(user),
            NextStatuses: next);
    }

    // Assigned is deliberately absent: ownership is claimed by accepting a
    // request, never by picking a status (FR-011, ADR-002).
    private static IReadOnlyList<RequestStatus> Allowed(CurrentUser user, RequestStatus from)
        => Enum.GetValues<RequestStatus>()
            .Where(to => to != RequestStatus.Assigned)
            .Where(to => StatusRules.CanMove(from, to))
            .Where(to => to != RequestStatus.Closed || user.Permissions.Contains(Permissions.Close))
            .ToArray();
}
