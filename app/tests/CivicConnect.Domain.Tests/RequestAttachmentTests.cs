using System.Text;
using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.Domain.Tests;

/// <summary>FR-006 (AC-006.1): allowed types and sizes are accepted, anything else refused.</summary>
[Trait("TestCase", "FR-006")]
public class RequestAttachmentTests
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01];
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    public static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%test");

    [Fact]
    public void Jpg_png_and_pdf_are_recognised_from_their_bytes()
    {
        Assert.Equal("image/png", RequestAttachment.ContentTypeOf(Png));
        Assert.Equal("image/jpeg", RequestAttachment.ContentTypeOf(Jpeg));
        Assert.Equal("application/pdf", RequestAttachment.ContentTypeOf(Pdf));
    }

    [Fact]
    public void A_renamed_file_of_another_type_is_refused()
    {
        var script = Encoding.ASCII.GetBytes("<script>alert(1)</script>");

        Assert.Equal(AttachmentCheck.TypeNotAllowed, RequestAttachment.Check(script.Length, script));
        Assert.Throws<ArgumentException>(() =>
            RequestAttachment.Create(Guid.NewGuid(), Guid.NewGuid(), "photo.png", script, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Empty_and_oversized_files_are_refused()
    {
        Assert.Equal(AttachmentCheck.Empty, RequestAttachment.Check(0, []));
        Assert.Equal(AttachmentCheck.TooLarge, RequestAttachment.Check(RequestAttachment.MaxBytes + 1, Png));
        Assert.Equal(AttachmentCheck.Ok, RequestAttachment.Check(RequestAttachment.MaxBytes, Png));
    }

    [Fact]
    public void An_allowed_file_is_stored_with_its_real_type_and_size()
    {
        var attachment = RequestAttachment.Create(Guid.NewGuid(), Guid.NewGuid(), "scan.pdf", Pdf, DateTimeOffset.UtcNow);

        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Equal(Pdf.Length, attachment.SizeBytes);
        Assert.Equal("scan.pdf", attachment.FileName);
    }

    [Theory]
    [InlineData(@"C:\Users\me\Pictures\pothole.png", "pothole.png")]
    [InlineData("../../etc/pothole.png", "pothole.png")]
    [InlineData("", "attachment.png")]
    [InlineData("say \"cheese\".png", "say cheese.png")]
    public void File_names_lose_folders_and_unsafe_characters(string given, string expected)
        => Assert.Equal(expected, RequestAttachment.CleanName(given, "image/png"));
}
