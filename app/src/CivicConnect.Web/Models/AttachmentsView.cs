using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Web.Models;

/// <summary>FR-006. A request's attachments, and whether this viewer may add one.</summary>
public sealed record AttachmentsView(Guid RequestId, IReadOnlyList<AttachmentInfo> Items, bool CanAttach)
{
    public static AttachmentsView None(Guid requestId) => new(requestId, [], false);

    /// <summary>The accept list for the file input. The server checks the bytes regardless.</summary>
    public const string Accept = ".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf";

    public static string Size(long bytes)
        => bytes < 1024 * 1024 ? $"{Math.Max(1, bytes / 1024)} KB" : $"{bytes / (1024.0 * 1024.0):0.0} MB";

    /// <summary>AC-006.1: a refused file is explained.</summary>
    public static string MessageFor(AttachmentOutcome outcome) => outcome switch
    {
        AttachmentOutcome.Added => "File attached.",
        AttachmentOutcome.Empty => "Choose a file to attach.",
        AttachmentOutcome.TooLarge => $"That file is too large. Files can be {RequestAttachment.MaxBytes / (1024 * 1024)} MB at most.",
        AttachmentOutcome.TypeNotAllowed => "Only JPG, PNG or PDF files can be attached.",
        AttachmentOutcome.TooMany => $"A request can have {RequestAttachment.MaxPerRequest} attachments at most.",
        AttachmentOutcome.RequestFinished => "Files can only be attached while the request is still open.",
        AttachmentOutcome.Forbidden => "You are not permitted to attach files to this request.",
        _ => "That request no longer exists."
    };
}
