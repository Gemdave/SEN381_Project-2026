using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Oversight;

/// <summary>
/// FR-015 and FR-017. Activity counts by status and by category for managers
/// and administrators (AC-015.1). Choosing a status or category updates the
/// counts and the list together (AC-017.1).
/// </summary>
public class IndexModel(
    IOversightQueries oversight,
    IRequestQueries queries,
    IAuthorisationPolicy policy,
    ICurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public RequestStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    public bool Allowed { get; private set; }
    public OversightSummary? Summary { get; private set; }
    public IReadOnlyList<CategoryOption> Categories { get; private set; } = [];

    public bool Filtered => Status is not null || CategoryId is not null;

    public async Task OnGetAsync(CancellationToken ct)
    {
        Allowed = await policy.MayViewOversightAsync(currentUser.Id, ct);
        if (!Allowed)
        {
            return;
        }

        Categories = await queries.ListActiveCategoriesAsync(ct);
        Summary = await oversight.SummaryAsync(new OversightFilter(Status, CategoryId), ct);
    }
}
