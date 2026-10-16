using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.Application.Tests;

/// <summary>
/// FR-001 and FR-002 at component level: UT-05, UT-02, and the BB-01 and
/// BB-02 partitions and boundaries (AC-001.1, AC-001.2, AC-002.2).
/// </summary>
public class SubmissionServiceTests
{
    private static readonly Guid Category = Guid.NewGuid();
    private static readonly Guid Requester = Guid.NewGuid();

    private static (SubmissionService Service, FakeRequestRepository Requests, FakeHistoryRepository History) Build(
        bool permitted = true)
    {
        var requests = new FakeRequestRepository(rowsAffected: 1, ownership: null);
        var history = new FakeHistoryRepository();
        var service = new SubmissionService(
            requests, history, new FakeQueries(Category), new FakePolicy(permitted), new DirectUnitOfWork(), new FixedClock());
        return (service, requests, history);
    }

    [Fact]
    [Trait("TestCase", "FR-016")]
    public async Task A_new_request_is_due_by_its_category_target()
    {
        var requests = new FakeRequestRepository(rowsAffected: 1, ownership: null);
        var service = new SubmissionService(
            requests, new FakeHistoryRepository(), new FakeQueries(Category) { TargetHours = 8 },
            new FakePolicy(true), new DirectUnitOfWork(), new FixedClock());

        await service.SubmitAsync(Requester, Valid(), CancellationToken.None);

        Assert.Equal(new FixedClock().UtcNow.AddHours(8), Assert.Single(requests.Added).TargetResponseAtUtc);
    }

    private static NewRequest Valid()
        => new("Broken corridor light", "The light outside room 12 flickers.", "Block B, floor 2", Category);

    [Fact]
    [Trait("TestCase", "UT-05")]
    public async Task Valid_requests_get_distinct_references_start_Received_and_belong_to_the_requester()
    {
        var (service, requests, history) = Build();

        var first = await service.SubmitAsync(Requester, Valid(), CancellationToken.None);
        var second = await service.SubmitAsync(Requester, Valid(), CancellationToken.None);

        Assert.Equal(SubmissionOutcome.Created, first.Outcome);
        Assert.Equal(SubmissionOutcome.Created, second.Outcome);
        Assert.NotEqual(first.Reference, second.Reference);
        Assert.All(requests.Added, r =>
        {
            Assert.Equal(RequestStatus.Received, r.Status);
            Assert.Equal(Requester, r.RequesterId);
            Assert.Equal(new FixedClock().UtcNow, r.CreatedAtUtc);
        });
        Assert.Equal(2, history.Entries.Count(e => e.Action == "Submitted"));
    }

    [Theory]
    [Trait("TestCase", "BB-01")]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("location")]
    [InlineData("categoryId")]
    public async Task A_missing_field_is_flagged_on_its_own_and_nothing_is_stored(string field)
    {
        var (service, requests, history) = Build();
        var input = field switch
        {
            "title" => Valid() with { Title = "   " },
            "description" => Valid() with { Description = null },
            "location" => Valid() with { Location = "" },
            _ => Valid() with { CategoryId = Guid.Empty }
        };

        var result = await service.SubmitAsync(Requester, input, CancellationToken.None);

        Assert.Equal(SubmissionOutcome.Invalid, result.Outcome);
        Assert.Equal(new[] { field }, result.Errors.Keys);
        Assert.Empty(requests.Added);
        Assert.Empty(history.Entries);
    }

    [Fact]
    [Trait("TestCase", "BB-01")]
    public async Task Every_missing_field_is_flagged_together()
    {
        var (service, requests, _) = Build();

        var result = await service.SubmitAsync(Requester, new NewRequest(null, null, null, Guid.Empty), CancellationToken.None);

        Assert.Equal(SubmissionOutcome.Invalid, result.Outcome);
        Assert.Equal(4, result.Errors.Count);
        Assert.Empty(requests.Added);
    }

    [Theory]
    [Trait("TestCase", "BB-02")]
    [InlineData(1, true)]
    [InlineData(Request.TitleMax - 1, true)]
    [InlineData(Request.TitleMax, true)]
    [InlineData(Request.TitleMax + 1, false)]
    public async Task Title_length_is_checked_at_the_boundary(int length, bool accepted)
    {
        var (service, _, _) = Build();

        var result = await service.SubmitAsync(
            Requester, Valid() with { Title = new string('a', length) }, CancellationToken.None);

        Assert.Equal(accepted ? SubmissionOutcome.Created : SubmissionOutcome.Invalid, result.Outcome);
    }

    [Fact]
    [Trait("TestCase", "UT-02")]
    public async Task A_category_that_is_not_on_the_list_is_rejected()
    {
        var (service, requests, _) = Build();

        var result = await service.SubmitAsync(
            Requester, Valid() with { CategoryId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Equal(SubmissionOutcome.Invalid, result.Outcome);
        Assert.Equal(new[] { "categoryId" }, result.Errors.Keys);
        Assert.Empty(requests.Added);
    }

    [Fact]
    [Trait("TestCase", "UT-04")]
    public async Task Someone_who_may_not_submit_is_refused_and_nothing_is_stored()
    {
        var (service, requests, history) = Build(permitted: false);

        var result = await service.SubmitAsync(Requester, Valid(), CancellationToken.None);

        Assert.Equal(SubmissionOutcome.Forbidden, result.Outcome);
        Assert.Empty(requests.Added);
        Assert.Empty(history.Entries);
    }
}
