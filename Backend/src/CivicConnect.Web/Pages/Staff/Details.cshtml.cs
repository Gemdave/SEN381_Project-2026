using CivicConnect.Core.Models;
using CivicConnect.Core.Services;
using CivicConnect.Web.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Staff;

/// <summary>
/// FR-010 for the view and FR-011 for the accept handler. This page is the user
/// facing end of the M2 demonstration trace: FR-011, ASR-01, ADR-002, TC-011.2.
/// </summary>
public class DetailsModel(
    RequestService requests,
    AssignmentService assignments,
    StatusTransitionService transitions,
    CapabilityFactory capabilities,
    PageUserAccessor pageUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ServiceRequest? Request { get; private set; }
    public IReadOnlyList<HistoryEntry> History { get; private set; } = [];
    public RequestCapabilities Capabilities { get; private set; } = RequestCapabilities.None;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        => await LoadAsync(ct);

    public async Task<IActionResult> OnPostAcceptAsync(CancellationToken ct)
    {
        var user = await pageUser.TryGetAsync(HttpContext, ct);
        if (user is null)
        {
            return RedirectToPage("/Index");
        }

        try
        {
            await assignments.AcceptAsync(user, Id, ct);
            ViewData["Message"] = "You now own this request.";
        }
        catch (ConflictException taken)
        {
            // AC-011.2. The conditional update in ADR-002 returned zero rows,
            // so somebody else owns it. The page says so rather than pretending.
            ViewData["Message"] = taken.Message;
        }
        catch (ForbiddenException denied)
        {
            ViewData["Message"] = denied.Message;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostTransitionAsync(RequestStatus next, CancellationToken ct)
    {
        var user = await pageUser.TryGetAsync(HttpContext, ct);
        if (user is null)
        {
            return RedirectToPage("/Index");
        }

        try
        {
            await transitions.TransitionAsync(user, Id, next, null, ct);
            ViewData["Message"] = "Status updated to " + StatusRules.Display(next) + ".";
        }
        catch (ValidationFailedException failed)
        {
            ViewData["Message"] = failed.Errors.SelectMany(e => e.Value).FirstOrDefault() ?? failed.Message;
        }
        catch (ConflictException conflict)
        {
            ViewData["Message"] = conflict.Message;
        }
        catch (ForbiddenException denied)
        {
            ViewData["Message"] = denied.Message;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return await LoadAsync(ct);
    }

    private async Task<IActionResult> LoadAsync(CancellationToken ct)
    {
        var user = await pageUser.TryGetAsync(HttpContext, ct);
        if (user is null)
        {
            return RedirectToPage("/Index");
        }

        try
        {
            var detail = await requests.GetAsync(user, Id, ct);
            Request = detail.Request;
            History = detail.History ?? [];
            Capabilities = capabilities.For(user, detail.Request);
            return Page();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
