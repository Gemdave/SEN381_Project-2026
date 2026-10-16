using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Requests;

public enum AttachmentOutcome
{
    Added,
    Forbidden,
    NotFound,
    Empty,
    TooLarge,
    TypeNotAllowed,
    TooMany,
    RequestFinished
}

/// <summary>
/// FR-006 (AC-006.1). An allowed file is stored and linked to the request; a
/// disallowed type or size is refused with a reason. Adding one writes a history
/// entry in the same save. Anyone who may view the request may see and download
/// its attachments, and nobody else (NFR-003).
/// </summary>
public sealed class AttachmentService(
    IAttachmentRepository attachments,
    IRequestRepository requests,
    IRequestHistoryRepository history,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<AttachmentOutcome> AddAsync(
        Guid requestId, Guid userId, string? fileName, Stream content, long length, CancellationToken ct)
    {
        if (!await policy.MayAsync(userId, RequestAction.Attach, requestId, ct))
        {
            return AttachmentOutcome.Forbidden;
        }

        // The declared length is checked before anything is read into memory.
        if (length <= 0)
        {
            return AttachmentOutcome.Empty;
        }

        if (length > RequestAttachment.MaxBytes)
        {
            return AttachmentOutcome.TooLarge;
        }

        var bytes = await ReadAtMostAsync(content, RequestAttachment.MaxBytes + 1, ct);
        switch (RequestAttachment.Check(bytes.LongLength, bytes))
        {
            case AttachmentCheck.Empty:
                return AttachmentOutcome.Empty;
            case AttachmentCheck.TooLarge:
                return AttachmentOutcome.TooLarge;
            case AttachmentCheck.TypeNotAllowed:
                return AttachmentOutcome.TypeNotAllowed;
        }

        var ownership = await requests.GetOwnershipAsync(requestId, ct);
        if (ownership is null)
        {
            return AttachmentOutcome.NotFound;
        }

        if (!ResponseTarget.IsOpen(ownership.Status))
        {
            return AttachmentOutcome.RequestFinished;
        }

        if (await attachments.CountForRequestAsync(requestId, ct) >= RequestAttachment.MaxPerRequest)
        {
            return AttachmentOutcome.TooMany;
        }

        var now = clock.UtcNow;
        var attachment = RequestAttachment.Create(requestId, userId, fileName, bytes, now);
        attachments.Add(attachment);
        history.Add(RequestHistoryEntry.ForAttachment(requestId, userId, attachment.FileName, now));
        await unitOfWork.SaveChangesAsync(ct);
        return AttachmentOutcome.Added;
    }

    public async Task<IReadOnlyList<AttachmentInfo>> ListAsync(Guid requestId, Guid userId, CancellationToken ct)
        => await policy.MayAsync(userId, RequestAction.View, requestId, ct)
            ? await attachments.ForRequestAsync(requestId, ct)
            : [];

    /// <summary>Null when it does not exist or this user may not view the request; the two look the same.</summary>
    public async Task<RequestAttachment?> FindAsync(Guid requestId, Guid attachmentId, Guid userId, CancellationToken ct)
        => await policy.MayAsync(userId, RequestAction.View, requestId, ct)
            ? await attachments.FindAsync(requestId, attachmentId, ct)
            : null;

    private static async Task<byte[]> ReadAtMostAsync(Stream content, int limit, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while (buffer.Length < limit && (read = await content.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, limit - buffer.Length)), ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
