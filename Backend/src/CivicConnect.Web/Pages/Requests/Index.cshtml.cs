using CivicConnect.Core.Models;
using CivicConnect.Core.Services;
using CivicConnect.Web.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// FR-004 and FR-005. The service scopes the query to this requester, so
/// another requester's rows are never loaded, let alone hidden (NFR-003).
/// </summary>
public class IndexModel(RequestService requests, PageUserAccessor pageUser) : PageModel
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
            var page = await requests.ListOwnAsync(user, Status, page: 1, pageSize: 20, ct: ct);
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
