namespace CivicConnect.Domain.Requests;

public enum AttachmentCheck
{
    Ok,
    Empty,
    TooLarge,
    TypeNotAllowed
}

/// <summary>
/// FR-006 (AC-006.1). Supporting information for a request, such as a photo.
/// Only JPG, PNG and PDF up to 5 MB are accepted, and the type is decided from
/// the file's own first bytes, not from its name or what the browser claims,
/// so a renamed file of another kind is refused.
/// </summary>
public class RequestAttachment
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public const int MaxPerRequest = 5;
    public const int FileNameMax = 100;

    private RequestAttachment() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequestId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public byte[] Content { get; private set; } = [];
    public DateTimeOffset UploadedAtUtc { get; private set; }

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static AttachmentCheck Check(long length, ReadOnlySpan<byte> content)
    {
        if (length <= 0)
        {
            return AttachmentCheck.Empty;
        }

        if (length > MaxBytes)
        {
            return AttachmentCheck.TooLarge;
        }

        return ContentTypeOf(content) is null ? AttachmentCheck.TypeNotAllowed : AttachmentCheck.Ok;
    }

    /// <summary>The media type the bytes really are, or null when it is not an allowed type.</summary>
    public static string? ContentTypeOf(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(JpegSignature))
        {
            return "image/jpeg";
        }

        if (content.StartsWith(PngSignature))
        {
            return "image/png";
        }

        return content.StartsWith("%PDF-"u8) ? "application/pdf" : null;
    }

    public static RequestAttachment Create(
        Guid requestId, Guid uploadedByUserId, string? fileName, byte[] content, DateTimeOffset now)
    {
        if (Check(content.LongLength, content) != AttachmentCheck.Ok)
        {
            throw new ArgumentException("Only JPG, PNG or PDF files up to 5 MB can be attached.", nameof(content));
        }

        return new RequestAttachment
        {
            RequestId = requestId,
            UploadedByUserId = uploadedByUserId,
            FileName = CleanName(fileName, ContentTypeOf(content)!),
            ContentType = ContentTypeOf(content)!,
            SizeBytes = content.LongLength,
            Content = content,
            UploadedAtUtc = now
        };
    }

    /// <summary>Drops any folder part and control characters, and keeps the name short.</summary>
    public static string CleanName(string? fileName, string contentType)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
        name = new string(name.Where(c => !char.IsControl(c) && c != '"').ToArray()).Trim();
        if (name.Length == 0)
        {
            name = "attachment" + contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                _ => ".pdf"
            };
        }

        return name.Length <= FileNameMax ? name : name[^FileNameMax..];
    }
}
