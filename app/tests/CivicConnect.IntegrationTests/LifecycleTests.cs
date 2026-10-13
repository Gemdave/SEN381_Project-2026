using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class LifecycleTests(TestDatabaseFixture fixture)
{
    [RequiresDatabaseFact]
    public async Task A_request_goes_from_received_to_closed_with_history_and_messages()
    {
        var data = await TestData.CreateAsync(fixture);
        await using var db = fixture.NewContext();
        var wired = new Wired(db);
        var ct = CancellationToken.None;

        var accepted = await wired.Assignment.AcceptAsync(data.RequestId, data.StaffId, ct);
        Assert.Equal(AssignmentOutcome.Assigned, accepted.Outcome);
        Assert.Equal(TransitionOutcome.Changed, await wired.Transitions.MoveAsync(data.RequestId, data.StaffId, RequestStatus.InProgress, null, ct));
        Assert.Equal(TransitionOutcome.Changed, await wired.Transitions.MoveAsync(data.RequestId, data.StaffId, RequestStatus.Resolved, "Fixed the fault.", ct));
        Assert.Equal(TransitionOutcome.Changed, await wired.Transitions.MoveAsync(data.RequestId, data.StaffId, RequestStatus.Closed, null, ct));

        await using var check = fixture.NewContext();
        Assert.Equal(RequestStatus.Closed, check.Requests.Single(r => r.Id == data.RequestId).Status);

        // Assigned, three status changes and the resolution note.
        Assert.Equal(5, check.RequestHistory.Count(h => h.RequestId == data.RequestId));

        // Accepted, in progress, resolved and closed: one message each.
        var messages = check.Feedback.Where(f => f.RequestId == data.RequestId).ToList();
        Assert.Equal(4, messages.Count);
        Assert.All(messages, m => Assert.Equal(data.RequesterId, m.RequesterId));
    }

    [RequiresDatabaseFact]
    public async Task Only_the_owner_or_an_administrator_can_close()
    {
        var data = await TestData.CreateAsync(fixture);
        await using var db = fixture.NewContext();
        var wired = new Wired(db);
        var ct = CancellationToken.None;

        await wired.Assignment.AcceptAsync(data.RequestId, data.StaffId, ct);
        await wired.Transitions.MoveAsync(data.RequestId, data.StaffId, RequestStatus.InProgress, null, ct);
        await wired.Transitions.MoveAsync(data.RequestId, data.StaffId, RequestStatus.Resolved, "Done.", ct);

        // Another staff member neither owns it nor is an administrator.
        Assert.Equal(TransitionOutcome.Forbidden, await wired.Transitions.MoveAsync(data.RequestId, data.OtherStaffId, RequestStatus.Closed, null, ct));

        // An administrator may close it.
        Assert.Equal(TransitionOutcome.Changed, await wired.Transitions.MoveAsync(data.RequestId, data.AdminId, RequestStatus.Closed, null, ct));
    }

    [RequiresDatabaseFact]
    public async Task Skipping_a_step_is_refused_by_the_service()
    {
        var data = await TestData.CreateAsync(fixture);
        await using var db = fixture.NewContext();
        var wired = new Wired(db);
        var ct = CancellationToken.None;

        await wired.Assignment.AcceptAsync(data.RequestId, data.StaffId, ct);

        Assert.Equal(TransitionOutcome.NotAllowed, await wired.Transitions.MoveAsync(data.RequestId, data.StaffId, RequestStatus.Closed, null, ct));
    }
}
