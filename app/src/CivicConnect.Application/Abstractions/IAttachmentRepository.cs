using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Abstractions;

/// <summary>An attachment without its content, for listing on a request page.</summary>
public sealed record AttachmentInfo(
    Guid Id, string FileName, string ContentType, long SizeBytes, Guid UploadedByUserId, DateTimeOffset UploadedAtUtc);

/// <summary>FR-006. Attachments are stored with the request in PostgreSQL (one deployable, one backup).</summary>
public interface IAttachmentRepository
{
    void Add(RequestAttachment attachment);

    Task<int> CountForRequestAsync(Guid requestId, CancellationToken ct);

    Task<IReadOnlyList<AttachmentInfo>> ForRequestAsync(Guid requestId, CancellationToken ct);

    /// <summary>Only an attachment that belongs to this request is returned.</summary>
    Task<RequestAttachment?> FindAsync(Guid requestId, Guid attachmentId, CancellationToken ct);
}
