namespace CivicConnect.Web.Models;

/// <summary>What this viewer may do. Built on the server, never in the browser.</summary>
public sealed record RequestCapabilities(
    bool CanAccept, bool CanTransition, bool CanAddNote, bool CanClose, bool CanReject = false, bool CanAttach = false)
{
    public static RequestCapabilities None => new(false, false, false, false);
}
