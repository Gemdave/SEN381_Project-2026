using CivicConnect.Core.Models;
using CivicConnect.Tests.Support;

namespace CivicConnect.Tests;

public class AssignmentServiceTests
{
    private readonly Scenario _s = new();

    [Fact]
    [Trait("TC", "TC-011.1")]
    public async Task Accepting_records_the_owner_history_and_tells_the_requester()
    {
        var rita = TestUsers.Requester();
        var sam = TestUsers.Staff();
        var request = await _s.Submit(rita);

        var updated = await _s.Assignments.AcceptAsync(sam, request.Id);

        Assert.Equal(sam.Id, updated.AssignedTo);
        Assert.NotNull(updated.AssignedAt);
        Assert.Equal(RequestStatus.Assigned, updated.Status);
        Assert.Contains(_s.Db.History, h => h.RequestId == request.Id && h.Action == "Assigned" && h.ActorId == sam.Id);

        var feedback = Assert.Single(_s.Db.Feedback);
        Assert.Equal(rita.Id, feedback.RecipientId);
        Assert.Equal(FeedbackKinds.Accepted, feedback.Kind);
    }

    [Fact]
    [Trait("TC", "TC-011.2")]
    public async Task A_second_staff_member_is_refused_and_the_first_stays_owner()
    {
        var request = await _s.Submit(TestUsers.Requester());
        var sam = TestUsers.Staff("Sam");
        var sipho = TestUsers.Staff("Sipho");
        await _s.Assignments.AcceptAsync(sam, request.Id);

        await Assert.ThrowsAsync<ConflictException>(() => _s.Assignments.AcceptAsync(sipho, request.Id));

        Assert.Equal(sam.Id, _s.Db.Requests[request.Id].AssignedTo);
        Assert.Single(_s.Db.History, h => h.Action == "Assigned");
        Assert.Single(_s.Db.Feedback);
    }

    [Fact]
    [Trait("TC", "TC-011.2")]
    public async Task Many_simultaneous_accepts_produce_exactly_one_winner()
    {
        var request = await _s.Submit(TestUsers.Requester());
        var staff = Enumerable.Range(0, 20).Select(i => TestUsers.Staff($"Staff{i}")).ToList();

        var outcomes = await Task.WhenAll(staff.Select(s => Task.Run(async () =>
        {
            try { await _s.Assignments.AcceptAsync(s, request.Id); return "won"; }
            catch (ConflictException) { return "refused"; }
        })));

        Assert.Equal(1, outcomes.Count(o => o == "won"));
        Assert.Equal(19, outcomes.Count(o => o == "refused"));
        Assert.Single(_s.Db.History, h => h.Action == "Assigned");
    }

    [Fact]
    public async Task A_requester_cannot_accept_requests()
    {
        var request = await _s.Submit(TestUsers.Requester());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _s.Assignments.AcceptAsync(TestUsers.Requester("Other"), request.Id));
    }

    [Fact]
    public async Task Accepting_a_request_that_does_not_exist_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _s.Assignments.AcceptAsync(TestUsers.Staff(), Guid.NewGuid()));
    }
}
