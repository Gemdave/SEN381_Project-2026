using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Models;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// FR-003 and FR-007. A request this user may not see answers exactly like a
/// missing one, so the page cannot confirm that someone else's request exists
/// (AC-004.2, NFR-003). The page has no status control at all (AC-003.2), and
/// it lists the timestamped updates written with each change (AC-007.1). The
/// status and updates refresh on their own every few seconds (NFR-002), and the
/// requester can attach supporting files while the request is open (FR-006).
/// </summary>
[RequestFormLimits(MultipartBodyLengthLimit = RequestAttachment.MaxBytes + 64 * 1024)]
public class DetailsModel(
    IRequestQueries queries,
    IFeedbackRepository feedback,
    AttachmentService attachments,
    IAuthorisationPolicy policy,
    ICurrentUser currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Submitted { get; set; }

    public RequestDetail? Detail { get; private set; }

    public IReadOnlyList<RequesterFeedback> Updates { get; private set; } = [];

    public AttachmentsView Attachments { get; private set; } = AttachmentsView.None(Guid.Empty);

    public PageMessage? Message { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        => await LoadAsync(ct) ? Page() : NotFound();

    /// <summary>NFR-002. The status and updates only, for status-poll.js to swap in.</summary>
    public async Task<IActionResult> OnGetUpdatesAsync(CancellationToken ct)
        => await LoadAsync(ct) ? Partial("_RequestUpdates", this) : NotFound();

    public async Task<IActionResult> OnPostAttachAsync(IFormFile? file, CancellationToken ct)
    {
        var outcome = file is null
            ? AttachmentOutcome.Empty
            : await attachments.AddAsync(Id, currentUser.Id, file.FileName, file.OpenReadStream(), file.Length, ct);

        Message = PageMessage.From(outcome == AttachmentOutcome.Added, AttachmentsView.MessageFor(outcome));
        return await LoadAsync(ct) ? Page() : NotFound();
    }

    private async Task<bool> LoadAsync(CancellationToken ct)
    {
        if (!await policy.MayAsync(currentUser.Id, RequestAction.View, Id, ct))
        {
            return false;
        }

        Detail = await queries.GetDetailAsync(Id, ct);
        if (Detail is null)
        {
            return false;
        }

        Updates = await feedback.ForRequestAsync(Id, ct);
        Attachments = new AttachmentsView(
            Id,
            await attachments.ListAsync(Id, currentUser.Id, ct),
            ResponseTarget.IsOpen(Detail.Status) && await policy.MayAsync(currentUser.Id, RequestAction.Attach, Id, ct));
        return true;
    }
}
