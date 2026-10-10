using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Web.Models;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Web.Pages.Staff;

/// <summary>
/// FR-010 for the view, FR-011 for the accept handler. This page is the user
/// facing end of the M2 demonstration trace.
/// </summary>
public class DetailsModel(
    CivicConnectDbContext db,
    AssignmentService assignment,
    IRequestHistoryRepository historyRepository,
    CapabilityFactory capabilities,
    ICurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public string Reference { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public RequestStatus Status { get; private set; }
    public RequestCapabilities Capabilities { get; private set; } = RequestCapabilities.None;
    public IReadOnlyList<RequestHistoryEntry> History { get; private set; } = [];
    public string? Message { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        => await LoadAsync(ct) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAcceptAsync(CancellationToken ct)
    {
        var result = await assignment.AcceptAsync(Id, currentUser.Id, ct);

        Message = result.Outcome switch
        {
            AssignmentOutcome.Assigned => "You now own this request.",
            AssignmentOutcome.AlreadyOwned => "Another staff member has already accepted this request.",
            AssignmentOutcome.Forbidden => "You are not permitted to accept requests.",
            _ => "That request no longer exists."
        };

        if (result.Outcome == AssignmentOutcome.NotFound)
        {
            return NotFound();
        }

        await LoadAsync(ct);
        return Page();
    }

    private async Task<bool> LoadAsync(CancellationToken ct)
    {
        var request = await db.Requests.AsNoTracking()
            .Where(r => r.Id == Id)
            .Select(r => new { r.Reference, r.Title, r.Description, r.Status })
            .FirstOrDefaultAsync(ct);

        if (request is null)
        {
            return false;
        }

        Reference = request.Reference;
        Title = request.Title;
        Description = request.Description;
        Status = request.Status;
        Capabilities = await capabilities.ForAsync(currentUser.Id, Id, ct);
        History = await historyRepository.ForRequestAsync(Id, ct);
        return true;
    }
}
