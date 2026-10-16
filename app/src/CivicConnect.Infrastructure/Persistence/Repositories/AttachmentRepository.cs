using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Repositories;

public sealed class AttachmentRepository(CivicConnectDbContext db) : IAttachmentRepository
{
    public void Add(RequestAttachment attachment) => db.Attachments.Add(attachment);

    public Task<int> CountForRequestAsync(Guid requestId, CancellationToken ct)
        => db.Attachments.CountAsync(a => a.RequestId == requestId, ct);

    /// <summary>The file content is not loaded for a list.</summary>
    public async Task<IReadOnlyList<AttachmentInfo>> ForRequestAsync(Guid requestId, CancellationToken ct)
        => await db.Attachments.AsNoTracking()
            .Where(a => a.RequestId == requestId)
            .OrderBy(a => a.UploadedAtUtc)
            .Select(a => new AttachmentInfo(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedByUserId, a.UploadedAtUtc))
            .ToListAsync(ct);

    public Task<RequestAttachment?> FindAsync(Guid requestId, Guid attachmentId, CancellationToken ct)
        => db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attachmentId && a.RequestId == requestId, ct);
}
