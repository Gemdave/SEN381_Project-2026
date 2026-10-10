using CivicConnect.Core.Models;
using CivicConnect.Core.Services;
using CivicConnect.Web.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// FR-003. A request this requester may not see is reported as missing rather
/// than refused, so the page cannot confirm that someone else's request exists
/// (NFR-003, AC-004.2).
/// </summary>
public class DetailsModel(RequestService requests, PageUserAccessor pageUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Submitted { get; set; }

    public ServiceRequest? Request { get; private set; }

    public bool JustSubmitted => Submitted;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
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
            return Page();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
