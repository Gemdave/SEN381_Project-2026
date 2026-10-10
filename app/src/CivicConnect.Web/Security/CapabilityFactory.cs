using CivicConnect.Application.Abstractions;
using CivicConnect.Web.Models;

namespace CivicConnect.Web.Security;

/// <summary>
/// ADR-004. The pages render controls from this, and ADR-005 authorises the
/// action again when the form posts. Hiding a button is a courtesy, not a control.
/// </summary>
public sealed class CapabilityFactory(IAuthorisationPolicy policy)
{
    public async Task<RequestCapabilities> ForAsync(Guid userId, Guid requestId, CancellationToken ct)
        => new(
            CanAccept: await policy.MayAsync(userId, RequestAction.Assign, requestId, ct),
            CanTransition: await policy.MayAsync(userId, RequestAction.Transition, requestId, ct),
            CanAddNote: await policy.MayAsync(userId, RequestAction.AddNote, requestId, ct),
            CanClose: await policy.MayAsync(userId, RequestAction.Close, requestId, ct));
}
