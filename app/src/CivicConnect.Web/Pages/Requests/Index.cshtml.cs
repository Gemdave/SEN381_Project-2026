using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// FR-004 and FR-005. The query is scoped to this requester, so another
/// requester's rows are never loaded, let alone hidden (AC-004.2, NFR-003). An
/// optional status filter narrows the list (AC-005.1).
/// </summary>
public class IndexModel(IRequestQueries queries, ICurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public RequestStatus? Status { get; set; }

    public IReadOnlyList<RequestListItem> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
        => Items = await queries.ListForRequesterAsync(currentUser.Id, Status, ct);
}
