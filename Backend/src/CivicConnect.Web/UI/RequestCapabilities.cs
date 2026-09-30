using CivicConnect.Core.Models;

namespace CivicConnect.Web.UI;

/// <summary>
/// ADR-004. What this viewer may do with this request, worked out on the server
/// and handed to the view. The view draws a control only when the capability is
/// present. It never decides whether the action is allowed: every handler asks
/// the authorisation policy again (ADR-005), so a hidden control is a courtesy
/// to the user and not a security control.
/// </summary>
public sealed record RequestCapabilities(
    bool CanAccept,
    bool CanTransition,
    bool CanSeeInternals,
    IReadOnlyList<RequestStatus> NextStatuses)
{
    public static RequestCapabilities None { get; } = new(false, false, false, []);
}
