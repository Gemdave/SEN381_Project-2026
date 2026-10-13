using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Models;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Staff;

/// <summary>
/// FR-010 for the view, FR-011 for the accept handler, FR-012 and FR-014 for
/// the status handler. This page is the user facing end of the M2
/// demonstration trace.
/// </summary>
public class DetailsModel(
    IRequestQueries queries,
    AssignmentService assignment,
    StatusTransitionService transitions,
    IRequestHistoryRepository historyRepository,
    IAuthorisationPolicy policy,
    CapabilityFactory capabilities,
    ICurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public RequestDetail? Detail { get; private set; }
    public RequestCapabilities Capabilities { get; private set; } = RequestCapabilities.None;
    public IReadOnlyList<RequestStatus> NextStatuses { get; private set; } = [];
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
            AssignmentOutcome.Forbidden => "You are not permitted to accept this request.",
            _ => "That request no longer exists."
        };

        if (result.Outcome == AssignmentOutcome.NotFound || !await LoadAsync(ct))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostTransitionAsync(RequestStatus next, string? note, CancellationToken ct)
    {
        var outcome = await transitions.MoveAsync(Id, currentUser.Id, next, note, ct);

        // AC-012.2: a refused move is explained, and the status stays as it was.
        Message = outcome switch
        {
            TransitionOutcome.Changed => "Status changed.",
            TransitionOutcome.NotAllowed => "That status change is not allowed from the current status.",
            TransitionOutcome.NoteRequired => "Resolving a request needs a note saying how it was resolved.",
            TransitionOutcome.Conflicted => "The status changed while you were working. Reload and try again.",
            TransitionOutcome.Forbidden => "You are not permitted to change this request's status.",
            _ => "That request no longer exists."
        };

        if (outcome == TransitionOutcome.NotFound || !await LoadAsync(ct))
        {
            return NotFound();
        }

        return Page();
    }

    private async Task<bool> LoadAsync(CancellationToken ct)
    {
        // AC-008.2: a request outside this user's scope answers exactly like a
        // missing one, so the page confirms nothing about it.
        if (!await policy.MayAsync(currentUser.Id, RequestAction.View, Id, ct))
        {
            return false;
        }

        Detail = await queries.GetDetailAsync(Id, ct);
        if (Detail is null)
        {
            return false;
        }

        Capabilities = await capabilities.ForAsync(currentUser.Id, Id, ct);

        // Only moves the domain allows (AC-012.1), and closing only with the
        // close capability (AC-014.2).
        NextStatuses = StatusTransition.NextFrom(Detail.Status)
            .Where(s => s == RequestStatus.Closed ? Capabilities.CanClose : Capabilities.CanTransition)
            .ToList();

        History = await historyRepository.ForRequestAsync(Id, ct);
        return true;
    }
}
