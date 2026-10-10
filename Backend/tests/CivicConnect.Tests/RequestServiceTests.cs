using CivicConnect.Core.Models;
using CivicConnect.Tests.Support;

namespace CivicConnect.Tests;

public class RequestServiceTests
{
    private readonly Scenario _s = new();

    [Fact]
    [Trait("TC", "TC-001.1")]
    public async Task Submitting_saves_a_Received_request_with_a_reference_and_history()
    {
        var rita = TestUsers.Requester();

        var request = await _s.Submit(rita);

        Assert.Equal(RequestStatus.Received, request.Status);
        Assert.StartsWith("CC-", request.Reference);
        Assert.Equal(rita.Id, request.RequesterId);
        Assert.Single(_s.Db.History, h => h.RequestId == request.Id && h.Action == "Submitted");
    }

    [Fact]
    [Trait("TC", "TC-001.2")]
    public async Task Missing_fields_are_each_reported_and_nothing_is_saved()
    {
        var blank = new NewRequest("  ", "", "", 0);

        var ex = await Assert.ThrowsAsync<ValidationFailedException>(
            () => _s.Requests.SubmitAsync(TestUsers.Requester(), blank));

        Assert.Contains("title", ex.Errors.Keys);
        Assert.Contains("description", ex.Errors.Keys);
        Assert.Contains("location", ex.Errors.Keys);
        Assert.Contains("categoryId", ex.Errors.Keys);
        Assert.Empty(_s.Db.Requests);
    }

    [Fact]
    [Trait("TC", "TC-002.1")]
    public async Task A_retired_category_cannot_be_chosen()
    {
        var retired = TestUsers.ValidRequest(categoryId: 3);

        var ex = await Assert.ThrowsAsync<ValidationFailedException>(
            () => _s.Requests.SubmitAsync(TestUsers.Requester(), retired));

        Assert.Contains("categoryId", ex.Errors.Keys);
    }

    [Fact]
    public async Task Overlong_title_is_refused()
    {
        var input = TestUsers.ValidRequest() with { Title = new string('x', 121) };

        var ex = await Assert.ThrowsAsync<ValidationFailedException>(
            () => _s.Requests.SubmitAsync(TestUsers.Requester(), input));

        Assert.Contains("title", ex.Errors.Keys);
    }

    [Fact]
    public async Task Staff_cannot_submit_requests_without_the_permission()
    {
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _s.Requests.SubmitAsync(TestUsers.Staff(), TestUsers.ValidRequest()));
    }

    [Fact]
    [Trait("TC", "TC-004.1")]
    public async Task A_requester_only_lists_their_own_requests()
    {
        var rita = TestUsers.Requester("Rita");
        var ravi = TestUsers.Requester("Ravi");
        await _s.Submit(rita);
        await _s.Submit(rita);
        await _s.Submit(ravi);

        var result = await _s.Requests.ListOwnAsync(rita, null, 1, 20);

        Assert.Equal(2, result.Total);
        Assert.All(result.Items, r => Assert.Equal(rita.Id, r.RequesterId));
    }

    [Fact]
    [Trait("TC", "TC-005.1")]
    public async Task Filtering_by_status_returns_only_matches()
    {
        var rita = TestUsers.Requester();
        var sam = TestUsers.Staff();
        await _s.Submit(rita);
        await _s.SubmitAndAccept(rita, sam);

        var received = await _s.Requests.ListOwnAsync(rita, RequestStatus.Received, 1, 20);
        var closed = await _s.Requests.ListOwnAsync(rita, RequestStatus.Closed, 1, 20);

        Assert.Single(received.Items);
        Assert.Empty(closed.Items);
    }

    [Fact]
    [Trait("TC", "TC-N003")]
    public async Task A_requester_asking_for_another_requesters_request_gets_not_found()
    {
        var rita = TestUsers.Requester("Rita");
        var ravi = TestUsers.Requester("Ravi");
        var ritas = await _s.Submit(rita);

        await Assert.ThrowsAsync<NotFoundException>(() => _s.Requests.GetAsync(ravi, ritas.Id));
    }

    [Fact]
    [Trait("TC", "TC-010.1")]
    public async Task Staff_see_history_and_notes_but_requesters_do_not()
    {
        var rita = TestUsers.Requester();
        var sam = TestUsers.Staff();
        var request = await _s.SubmitAndAccept(rita, sam);
        await _s.Notes.AddAsync(sam, request.Id, "On my way.");

        var staffView = await _s.Requests.GetAsync(sam, request.Id);
        var requesterView = await _s.Requests.GetAsync(rita, request.Id);

        Assert.Equal(2, staffView.History!.Count);
        Assert.Single(staffView.Notes!);
        Assert.Null(requesterView.History);
        Assert.Null(requesterView.Notes);
    }

    [Fact]
    [Trait("TC", "TC-008.1")]
    public async Task The_queue_shows_unowned_requests_and_my_own_but_not_a_colleagues()
    {
        var rita = TestUsers.Requester();
        var sam = TestUsers.Staff("Sam");
        var sipho = TestUsers.Staff("Sipho");
        var samsRequest = await _s.SubmitAndAccept(rita, sam);
        var open = await _s.Submit(rita);

        var siphoQueue = await _s.Requests.QueueAsync(sipho, null, null, null, 1, 20);
        var samQueue = await _s.Requests.QueueAsync(sam, null, null, null, 1, 20);

        Assert.Contains(siphoQueue.Items, r => r.Id == open.Id);
        Assert.DoesNotContain(siphoQueue.Items, r => r.Id == samsRequest.Id);
        Assert.Contains(samQueue.Items, r => r.Id == samsRequest.Id);
    }

    [Fact]
    public async Task The_queue_is_closed_to_requesters()
    {
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _s.Requests.QueueAsync(TestUsers.Requester(), null, null, null, 1, 20));
    }

    [Fact]
    [Trait("TC", "TC-009.1")]
    public async Task An_unknown_sort_value_is_refused()
    {
        var ex = await Assert.ThrowsAsync<ValidationFailedException>(
            () => _s.Requests.QueueAsync(TestUsers.Staff(), null, null, "name; DROP TABLE request", 1, 20));

        Assert.Contains("sort", ex.Errors.Keys);
    }
}
