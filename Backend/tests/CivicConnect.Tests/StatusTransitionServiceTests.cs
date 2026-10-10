using CivicConnect.Core.Models;
using CivicConnect.Tests.Support;

namespace CivicConnect.Tests;

public class StatusTransitionServiceTests
{
    private readonly Scenario _s = new();

    [Fact]
    [Trait("TC", "TC-012.1")]
    public async Task The_owner_can_walk_a_request_through_to_Closed_and_each_step_is_recorded()
    {
        var rita = TestUsers.Requester();
        var sam = TestUsers.Staff();
        var request = await _s.SubmitAndAccept(rita, sam);

        await _s.Transitions.TransitionAsync(sam, request.Id, RequestStatus.InProgress, null);
        await _s.Transitions.TransitionAsync(sam, request.Id, RequestStatus.Resolved, null);
        var closed = await _s.Transitions.TransitionAsync(sam, request.Id, RequestStatus.Closed, null);

        Assert.Equal(RequestStatus.Closed, closed.Status);
        Assert.Equal(3, _s.Db.History.Count(h => h.Action == "StatusChanged" && h.ActorId == sam.Id));
        Assert.Contains(_s.Db.Feedback, f => f.Kind == FeedbackKinds.Completed);
    }

    [Fact]
    [Trait("TC", "TC-012.2")]
    public async Task An_invalid_move_is_refused_with_a_message_and_nothing_is_written()
    {
        var sam = TestUsers.Staff();
        var request = await _s.Submit(TestUsers.Requester());
        var historyBefore = _s.Db.History.Count;

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => _s.Transitions.TransitionAsync(sam, request.Id, RequestStatus.Closed, null));

        Assert.Contains("Received", ex.Message);
        Assert.Contains("Closed", ex.Message);
        Assert.Equal(RequestStatus.Received, _s.Db.Requests[request.Id].Status);
        Assert.Equal(historyBefore, _s.Db.History.Count);
        Assert.Empty(_s.Db.Feedback);
    }

    [Fact]
    public async Task The_status_endpoint_cannot_be_used_to_take_ownership()
    {
        var request = await _s.Submit(TestUsers.Requester());

        var ex = await Assert.ThrowsAsync<ValidationFailedException>(
            () => _s.Transitions.TransitionAsync(TestUsers.Staff(), request.Id, RequestStatus.Assigned, null));

        Assert.Contains("status", ex.Errors.Keys);
    }

    [Fact]
    [Trait("TC", "TC-007.2")]
    public async Task Rejecting_needs_a_reason_and_the_requester_sees_it()
    {
        var rita = TestUsers.Requester();
        var sam = TestUsers.Staff();
        var request = await _s.Submit(rita);

        await Assert.ThrowsAsync<ValidationFailedException>(
            () => _s.Transitions.TransitionAsync(sam, request.Id, RequestStatus.Rejected, "  "));

        await _s.Transitions.TransitionAsync(sam, request.Id, RequestStatus.Rejected, "Outside our service.");

        var feedback = Assert.Single(await _s.Feedback.ListAsync(rita));
        Assert.Equal(FeedbackKinds.Rejected, feedback.Kind);
        Assert.Equal("Outside our service.", feedback.Reason);
    }

    [Fact]
    [Trait("TC", "TC-014.2")]
    public async Task Staff_without_the_close_permission_cannot_close()
    {
        var sam = TestUsers.Staff();
        var request = await _s.SubmitAndAccept(TestUsers.Requester(), sam);
        await _s.Transitions.TransitionAsync(sam, request.Id, RequestStatus.InProgress, null);
        await _s.Transitions.TransitionAsync(sam, request.Id, RequestStatus.Resolved, null);

        // Same person, but with the close permission removed.
        var noClose = new CurrentUser(sam.Id, sam.DisplayName,
            sam.Permissions.Where(p => p != Permissions.Close).ToHashSet());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _s.Transitions.TransitionAsync(noClose, request.Id, RequestStatus.Closed, null));

        Assert.Equal(RequestStatus.Resolved, _s.Db.Requests[request.Id].Status);
    }

    [Fact]
    public async Task Another_staff_member_cannot_change_a_request_they_do_not_own()
    {
        var sam = TestUsers.Staff("Sam");
        var sipho = TestUsers.Staff("Sipho");
        var request = await _s.SubmitAndAccept(TestUsers.Requester(), sam);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _s.Transitions.TransitionAsync(sipho, request.Id, RequestStatus.InProgress, null));
    }

    [Fact]
    public async Task A_requester_cannot_change_status()
    {
        var rita = TestUsers.Requester();
        var request = await _s.Submit(rita);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _s.Transitions.TransitionAsync(rita, request.Id, RequestStatus.InProgress, null));
    }
}
