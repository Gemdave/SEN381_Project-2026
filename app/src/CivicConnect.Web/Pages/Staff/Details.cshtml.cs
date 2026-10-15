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
/// the status handler, FR-013 for the note handler, CR-009 for the reject
/// handler and FR-006 for attachments. This page is the user facing end of the
/// M2 demonstration trace.
/// </summary>
[RequestFormLimits(MultipartBodyLengthLimit = RequestAttachment.MaxBytes + 64 * 1024)]
public class DetailsModel(
    IRequestQueries queries,
    AssignmentService assignment,
    StatusTransitionService transitions,
    NoteService notes,
    AttachmentService attachments,
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
    public HistoryView History { get; private set; } = new([], new Dictionary<Guid, string>());
    public AttachmentsView Attachments { get; private set; } = AttachmentsView.None(Guid.Empty);
    public PageMessage? Message { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        => await LoadAsync(ct) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAcceptAsync(CancellationToken ct)
    {
        var result = await assignment.AcceptAsync(Id, currentUser.Id, ct);

        Message = PageMessage.From(result.Outcome == AssignmentOutcome.Assigned, result.Outcome switch
        {
            AssignmentOutcome.Assigned => "You now own this request.",
            AssignmentOutcome.AlreadyOwned => "Another staff member has already accepted this request.",
            AssignmentOutcome.Forbidden => "You are not permitted to accept this request.",
            _ => "That request no longer exists."
        });

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
        Message = PageMessage.From(outcome == TransitionOutcome.Changed, outcome switch
        {
            TransitionOutcome.Changed => "Status changed.",
            TransitionOutcome.NotAllowed => "That status change is not allowed from the current status.",
            TransitionOutcome.NoteRequired => "Resolving a request needs a note saying how it was resolved.",
            TransitionOutcome.Conflicted => "The status changed while you were working. Reload and try again.",
            TransitionOutcome.Forbidden => "You are not permitted to change this request's status.",
            _ => "That request no longer exists."
        });

        if (outcome == TransitionOutcome.NotFound || !await LoadAsync(ct))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(string? reason, CancellationToken ct)
    {
        // CR-009 (AC-007.2): the reason is required and goes to the requester.
        var outcome = await transitions.RejectAsync(Id, currentUser.Id, reason, ct);

        Message = PageMessage.From(outcome == TransitionOutcome.Changed, outcome switch
        {
            TransitionOutcome.Changed => "Request rejected. The requester has been told why.",
            TransitionOutcome.NoteRequired => "Rejecting a request needs a reason for the requester.",
            TransitionOutcome.NotAllowed => "Only a request that has just been received can be rejected.",
            TransitionOutcome.Conflicted => "The request changed while you were working. Reload and try again.",
            TransitionOutcome.Forbidden => "You are not permitted to reject this request.",
            _ => "That request no longer exists."
        });

        if (outcome == TransitionOutcome.NotFound || !await LoadAsync(ct))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAttachAsync(IFormFile? file, CancellationToken ct)
    {
        var outcome = file is null
            ? AttachmentOutcome.Empty
            : await attachments.AddAsync(Id, currentUser.Id, file.FileName, file.OpenReadStream(), file.Length, ct);

        Message = PageMessage.From(outcome == AttachmentOutcome.Added, AttachmentsView.MessageFor(outcome));
        return await LoadAsync(ct) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostNoteAsync(string? text, CancellationToken ct)
    {
        // FR-013 (AC-013.1): saved to the history with author and time, never edited.
        var outcome = await notes.AddAsync(Id, currentUser.Id, text, ct);

        Message = PageMessage.From(outcome == NoteOutcome.Added, outcome switch
        {
            NoteOutcome.Added => "Note added.",
            NoteOutcome.Empty => "A note cannot be empty.",
            NoteOutcome.TooLong => $"A note can be {NoteService.MaxLength} characters at most.",
            _ => "You are not permitted to add notes to this request."
        });

        return await LoadAsync(ct) ? Page() : NotFound();
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
        // close capability (AC-014.2). Rejecting has its own form (CR-009).
        NextStatuses = StatusTransition.NextFrom(Detail.Status)
            .Where(s => s != RequestStatus.Rejected)
            .Where(s => s == RequestStatus.Closed ? Capabilities.CanClose : Capabilities.CanTransition)
            .ToList();

        Attachments = new AttachmentsView(
            Id,
            await attachments.ListAsync(Id, currentUser.Id, ct),
            ResponseTarget.IsOpen(Detail.Status) && Capabilities.CanAttach);

        var entries = await historyRepository.ForRequestAsync(Id, ct);
        History = new HistoryView(entries, await queries.DisplayNamesAsync(HistoryView.PeopleIn(entries), ct));
        return true;
    }
}
