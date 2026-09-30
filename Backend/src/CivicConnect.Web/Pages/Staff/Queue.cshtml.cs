using CivicConnect.Core.Models;
using CivicConnect.Core.Services;
using CivicConnect.Web.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Staff;

/// <summary>
/// FR-008 and FR-009. Scope is applied in the query by the service, so requests
/// this staff member may not see never reach the page (AC-008.2).
/// </summary>
public class QueueModel(RequestService requests, PageUserAccessor pageUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public RequestStatus? Status { get; set; }

    public IReadOnlyList<ServiceRequest> Items { get; private set; } = [];
    public int Total { get; private set; }

    public RequestStatus[] StatusOptions { get; } = Enum.GetValues<RequestStatus>();

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var user = await pageUser.TryGetAsync(HttpContext, ct);
        if (user is null)
        {
            return RedirectToPage("/Index");
        }

        try
        {
            var page = await requests.QueueAsync(user, Status, null, "created", page: 1, pageSize: 20, ct);
            Items = page.Items;
            Total = page.Total;
            return Page();
        }
        catch (ForbiddenException denied)
        {
            ViewData["Message"] = denied.Message;
            return Page();
        }
    }
}
