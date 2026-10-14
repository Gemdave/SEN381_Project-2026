using CivicConnect.Application.Abstractions;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Reports;

/// <summary>
/// FR-023 (AC-023.1, CR-007). High-level service performance for sponsors,
/// managers and administrators. It reuses the management figures at summary
/// level, so the two cannot disagree (NFR-008), and it lists no individual
/// requests, so a sponsor sees no requester's details (NFR-003).
/// </summary>
public class SummaryModel(
    IOversightQueries oversight,
    IAuthorisationPolicy policy,
    ICurrentUser currentUser) : PageModel
{
    public bool Allowed { get; private set; }
    public OversightSummary? Summary { get; private set; }

    /// <summary>Share of resolved requests that met their target, or null when none are resolved yet.</summary>
    public int? WithinTargetPercent
        => Summary is { Resolved: > 0 } s ? (int)Math.Round(100.0 * s.ResolvedWithinTarget / s.Resolved) : null;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Allowed = await policy.MayViewSummaryAsync(currentUser.Id, ct);
        if (Allowed)
        {
            Summary = await oversight.SummaryAsync(new OversightFilter(), ct);
        }
    }
}
