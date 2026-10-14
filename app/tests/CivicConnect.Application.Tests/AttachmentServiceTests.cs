using System.Text;
using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.Application.Tests;

internal sealed class FakeAttachmentRepository : IAttachmentRepository
{
    public List<RequestAttachment> Items { get; } = [];
    public int ExistingCount { get; init; }

    public void Add(RequestAttachment attachment) => Items.Add(attachment);

    public Task<int> CountForRequestAsync(Guid requestId, CancellationToken ct)
        => Task.FromResult(ExistingCount + Items.Count);

    public Task<IReadOnlyList<AttachmentInfo>> ForRequestAsync(Guid requestId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<AttachmentInfo>>(Items
            .Select(a => new AttachmentInfo(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedByUserId, a.UploadedAtUtc))
            .ToList());

    public Task<RequestAttachment?> FindAsync(Guid requestId, Guid attachmentId, CancellationToken ct)
        => Task.FromResult(Items.FirstOrDefault(a => a.Id == attachmentId));
}

/// <summary>FR-006 (AC-006.1): an allowed file is stored and linked; anything else is refused with a reason.</summary>
[Trait("TestCase", "FR-006")]
public class AttachmentServiceTests
{
    private static readonly Guid User = Guid.NewGuid();
    private static readonly Guid RequestId = Guid.NewGuid();
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01];

    private static (AttachmentService Service, FakeAttachmentRepository Attachments, FakeHistoryRepository History) Build(
        RequestStatus status = RequestStatus.Received,
        IAuthorisationPolicy? policy = null,
        int existing = 0,
        bool exists = true)
    {
        var attachments = new FakeAttachmentRepository { ExistingCount = existing };
        var history = new FakeHistoryRepository();
        var ownership = exists ? new RequestOwnership(RequestId, null, status, User) : null;
        var service = new AttachmentService(
            attachments,
            new FakeRequestRepository(1, ownership),
            history,
            policy ?? new FakePolicy(true),
            new DirectUnitOfWork(),
            new FixedClock());
        return (service, attachments, history);
    }

    private static Task<AttachmentOutcome> AddAsync(AttachmentService service, byte[] content, string name = "pothole.png", long? length = null)
        => service.AddAsync(RequestId, User, name, new MemoryStream(content), length ?? content.Length, CancellationToken.None);

    [Fact]
    public async Task An_allowed_file_is_stored_and_recorded_in_the_history()
    {
        var (service, attachments, history) = Build();

        Assert.Equal(AttachmentOutcome.Added, await AddAsync(service, Png));

        var stored = Assert.Single(attachments.Items);
        Assert.Equal(("pothole.png", "image/png", RequestId), (stored.FileName, stored.ContentType, stored.RequestId));
        var entry = Assert.Single(history.Entries);
        Assert.Equal(("AttachmentAdded", "pothole.png"), (entry.Action, entry.NewValue));
    }

    [Fact]
    public async Task A_disallowed_type_is_refused_even_with_an_allowed_name()
    {
        var (service, attachments, history) = Build();

        Assert.Equal(AttachmentOutcome.TypeNotAllowed, await AddAsync(service, Encoding.ASCII.GetBytes("MZ fake exe"), "photo.png"));
        Assert.Empty(attachments.Items);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task An_oversized_file_is_refused_before_it_is_read()
    {
        var (service, attachments, _) = Build();
        var unreadable = new ThrowingStream();

        var outcome = await service.AddAsync(
            RequestId, User, "big.png", unreadable, RequestAttachment.MaxBytes + 1, CancellationToken.None);

        Assert.Equal(AttachmentOutcome.TooLarge, outcome);
        Assert.Empty(attachments.Items);
    }

    [Fact]
    public async Task An_empty_file_is_refused()
    {
        var (service, _, _) = Build();

        Assert.Equal(AttachmentOutcome.Empty, await AddAsync(service, []));
    }

    [Fact]
    public async Task Without_the_attach_permission_nothing_is_stored()
    {
        var (service, attachments, _) = Build(policy: new DenyActionPolicy(RequestAction.Attach));

        Assert.Equal(AttachmentOutcome.Forbidden, await AddAsync(service, Png));
        Assert.Empty(attachments.Items);
    }

    [Theory]
    [InlineData(RequestStatus.Resolved)]
    [InlineData(RequestStatus.Closed)]
    [InlineData(RequestStatus.Rejected)]
    public async Task A_finished_request_takes_no_more_files(RequestStatus status)
    {
        var (service, attachments, _) = Build(status);

        Assert.Equal(AttachmentOutcome.RequestFinished, await AddAsync(service, Png));
        Assert.Empty(attachments.Items);
    }

    [Fact]
    public async Task A_request_holds_at_most_the_limit()
    {
        var (service, attachments, _) = Build(existing: RequestAttachment.MaxPerRequest);

        Assert.Equal(AttachmentOutcome.TooMany, await AddAsync(service, Png));
        Assert.Empty(attachments.Items);
    }

    [Fact]
    public async Task A_missing_request_is_reported_as_not_found()
    {
        var (service, _, _) = Build(exists: false);

        Assert.Equal(AttachmentOutcome.NotFound, await AddAsync(service, Png));
    }

    [Fact]
    public async Task Someone_who_may_not_view_the_request_cannot_list_or_download_its_files()
    {
        var (service, attachments, _) = Build();
        await AddAsync(service, Png);
        var stored = attachments.Items[0];

        // The same stored file, asked for by someone the policy refuses.
        var refused = new AttachmentService(
            attachments,
            new FakeRequestRepository(1, new RequestOwnership(RequestId, null, RequestStatus.Received, User)),
            new FakeHistoryRepository(),
            new DenyActionPolicy(RequestAction.View),
            new DirectUnitOfWork(),
            new FixedClock());

        Assert.Single(await service.ListAsync(RequestId, User, CancellationToken.None));
        Assert.NotNull(await service.FindAsync(RequestId, stored.Id, User, CancellationToken.None));
        Assert.Empty(await refused.ListAsync(RequestId, User, CancellationToken.None));
        Assert.Null(await refused.FindAsync(RequestId, stored.Id, User, CancellationToken.None));
    }

    private sealed class ThrowingStream : MemoryStream
    {
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new InvalidOperationException("The content should not have been read.");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("The content should not have been read.");
    }
}
