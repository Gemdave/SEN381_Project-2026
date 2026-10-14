using CivicConnect.Application.Requests;
using CivicConnect.Web.Security;

namespace CivicConnect.Web.Endpoints;

/// <summary>
/// FR-006. Downloads an attachment for anyone who may view its request; anyone
/// else gets 404, exactly as for a missing file (NFR-003). Files are always sent
/// as downloads and never sniffed, so an upload cannot run in the browser as a
/// page. Uploads go through the request pages, which carry the antiforgery token.
/// </summary>
public static class AttachmentEndpoints
{
    public static void MapAttachmentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/requests/{id:guid}/attachments/{attachmentId:guid}", async (
            Guid id,
            Guid attachmentId,
            AttachmentService service,
            ICurrentUser currentUser,
            HttpContext http,
            CancellationToken ct) =>
        {
            var attachment = await service.FindAsync(id, attachmentId, currentUser.Id, ct);
            if (attachment is null)
            {
                return Results.NotFound();
            }

            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(attachment.Content, attachment.ContentType, attachment.FileName);
        });
    }
}
