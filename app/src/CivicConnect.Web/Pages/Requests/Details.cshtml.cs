using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// FR-003 and FR-007. A request this user may not see answers exactly like a
/// missing one, so the page cannot confirm that someone else's request exists
/// (AC-004.2, NFR-003). The page has no status control at all (AC-003.2), and
/// it lists the timestamped updates written with each change (AC-007.1).
/// </summary>
public class DetailsModel(
    IRequestQueries queries,
    IFeedbackRepository feedback,
    IAuthorisationPolicy policy,
    ICurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Submitted { get; set; }

    public RequestDetail? Detail { get; private set; }

    public IReadOnlyList<RequesterFeedback> Updates { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!await policy.MayAsync(currentUser.Id, RequestAction.View, Id, ct))
        {
            return NotFound();
        }

        Detail = await queries.GetDetailAsync(Id, ct);
        if (Detail is null)
        {
            return NotFound();
        }

        Updates = await feedback.ForRequestAsync(Id, ct);
        return Page();
    }
}
