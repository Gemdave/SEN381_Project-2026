using CivicConnect.Application.Abstractions;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// FR-003. A request this user may not see answers exactly like a missing one,
/// so the page cannot confirm that someone else's request exists (AC-004.2,
/// NFR-003). The page has no status control at all (AC-003.2).
/// </summary>
public class DetailsModel(IRequestQueries queries, IAuthorisationPolicy policy, ICurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Submitted { get; set; }

    public RequestDetail? Detail { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!await policy.MayAsync(currentUser.Id, RequestAction.View, Id, ct))
        {
            return NotFound();
        }

        Detail = await queries.GetDetailAsync(Id, ct);
        return Detail is null ? NotFound() : Page();
    }
}
