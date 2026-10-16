using System.Net;
using System.Net.Http.Json;
using System.Text;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// FR-006 (AC-006.1) through the real pages: an allowed file is stored and
/// linked to the request, a disallowed type or size is refused with a message,
/// and only people who may view the request can download it (NFR-003).
/// </summary>
[Collection(WebAppCollection.Name)]
public class AttachmentTests(WebAppFixture app)
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n% a test document\n");

    private sealed record Created(Guid Id, string Reference);

    private async Task<Created> SubmitFaultAsync(HttpClient requester, string title)
    {
        var response = await requester.PostAsJsonAsync("/api/v1/requests", new
        {
            title,
            description = title + ", raised by an attachment test.",
            location = "Bus stop 4",
            categoryId = await app.CategoryIdAsync("Fault")
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Created>())!;
    }

    private static async Task<string> UploadAsync(HttpClient client, string page, Guid requestId, string fileName, byte[] content)
    {
        var token = Html.Token(await client.GetStringAsync($"{page}?id={requestId}"));
        using var form = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(requestId.ToString()), "id" },
            { new ByteArrayContent(content), "file", fileName }
        };

        var response = await client.PostAsync($"{page}?id={requestId}&handler=Attach", form);
        return Html.Message(await response.Content.ReadAsStringAsync()) ?? "(no message)";
    }

    private Task<Guid> AttachmentIdAsync(Guid requestId, string fileName)
        => app.QueryAsync(db => db.Attachments.Where(a => a.RequestId == requestId && a.FileName == fileName).Select(a => a.Id).SingleAsync());

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-006.1")]
    public async Task A_requester_attaches_a_photo_and_staff_in_scope_can_download_it()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var created = await SubmitFaultAsync(requester, "Pothole on the corner");

        Assert.Equal("File attached.", await UploadAsync(requester, "/Requests/Details", created.Id, "pothole.png", Png));
        Assert.Contains(">pothole.png</a>", Html.BodyOf(await requester.GetStringAsync("/Requests/Details?id=" + created.Id)));

        var id = await AttachmentIdAsync(created.Id, "pothole.png");
        var staff = await app.ClientForAsync(DevUsers.StaffOne);
        var download = await staff.GetAsync($"/api/v1/requests/{created.Id}/attachments/{id}");

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(Png, await download.Content.ReadAsByteArrayAsync());

        // The upload is in the request's history (FR-022).
        Assert.True(await app.QueryAsync(db => db.RequestHistory.AnyAsync(h =>
            h.RequestId == created.Id && h.Action == "AttachmentAdded" && h.NewValue == "pothole.png")));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-006.1")]
    public async Task A_disallowed_type_or_size_is_refused_with_a_message()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var created = await SubmitFaultAsync(requester, "Graffiti on the wall");

        var renamed = Encoding.ASCII.GetBytes("<html><script>alert(1)</script></html>");
        Assert.Equal("Only JPG, PNG or PDF files can be attached.",
            await UploadAsync(requester, "/Requests/Details", created.Id, "innocent.png", renamed));

        var oversized = new byte[RequestAttachment.MaxBytes + 1];
        Png.CopyTo(oversized, 0);
        Assert.Equal("That file is too large. Files can be 5 MB at most.",
            await UploadAsync(requester, "/Requests/Details", created.Id, "huge.png", oversized));

        Assert.Equal(0, await app.QueryAsync(db => db.Attachments.CountAsync(a => a.RequestId == created.Id)));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "NFR-003")]
    public async Task Another_requester_cannot_download_or_attach()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var created = await SubmitFaultAsync(requester, "Broken swing");
        await UploadAsync(requester, "/Requests/Details", created.Id, "swing.png", Png);
        var id = await AttachmentIdAsync(created.Id, "swing.png");

        var other = await app.ClientForAsync(DevUsers.RequesterTwo);

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/requests/{created.Id}/attachments/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/Requests/Details?id=" + created.Id)).StatusCode);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-006.1")]
    public async Task Staff_attach_only_once_they_own_the_request()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var created = await SubmitFaultAsync(requester, "Faulty street lamp");
        var staff = await app.ClientForAsync(DevUsers.StaffOne);

        Assert.DoesNotContain("handler=Attach", await staff.GetStringAsync("/Staff/Details?id=" + created.Id));
        Assert.Equal("You are not permitted to attach files to this request.",
            await UploadAsync(staff, "/Staff/Details", created.Id, "report.pdf", Pdf));

        (await staff.PostAsync($"/api/v1/requests/{created.Id}/assignment", null)).EnsureSuccessStatusCode();

        Assert.Equal("File attached.", await UploadAsync(staff, "/Staff/Details", created.Id, "report.pdf", Pdf));
        Assert.Contains(">report.pdf</a>", Html.BodyOf(await requester.GetStringAsync("/Requests/Details?id=" + created.Id)));
    }
}
