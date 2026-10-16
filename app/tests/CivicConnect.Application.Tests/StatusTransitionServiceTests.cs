using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.Application.Tests;

internal sealed class FakeFeedbackRepository : IFeedbackRepository
{
    public List<RequesterFeedback> Items { get; } = [];

    public void Add(RequesterFeedback feedback) => Items.Add(feedback);

    public Task<IReadOnlyList<RequesterFeedback>> ForRequestAsync(Guid requestId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<RequesterFeedback>>(Items);
}

internal sealed class DenyActionPolicy(RequestAction denied) : IAuthorisationPolicy
{
    public Task<bool> MayAsync(Guid userId, RequestAction action, Guid requestId, CancellationToken ct)
        => Task.FromResult(action != denied);
}

public class StatusTransitionServiceTests
{
    private static readonly Guid Staff = Guid.NewGuid();
    private static readonly Guid Requester = Guid.NewGuid();

    private static (StatusTransitionService Service, FakeHistoryRepository History, FakeFeedbackRepository Feedback) Build(
        RequestStatus current, int rows = 1, IAuthorisationPolicy? policy = null)
    {
        var ownership = new RequestOwnership(Guid.NewGuid(), Staff, current, Requester);
        var history = new FakeHistoryRepository();
        var feedback = new FakeFeedbackRepository();
        var service = new StatusTransitionService(
            new FakeRequestRepository(rows, ownership),
            history,
            policy ?? new FakePolicy(true),
            new DirectUnitOfWork(),
            new FixedClock(),
            feedback);
        return (service, history, feedback);
    }

    [Fact]
    public async Task An_invalid_transition_is_refused_and_nothing_is_recorded()
    {
        var (service, history, feedback) = Build(RequestStatus.Received);

        var outcome = await service.MoveAsync(Guid.NewGuid(), Staff, RequestStatus.Closed, null, CancellationToken.None);

        Assert.Equal(TransitionOutcome.NotAllowed, outcome);
        Assert.Empty(history.Entries);
        Assert.Empty(feedback.Items);
    }

    [Fact]
    public async Task Closing_needs_the_close_permission()
    {
        var (service, history, _) = Build(RequestStatus.Resolved, policy: new DenyActionPolicy(RequestAction.Close));

        var outcome = await service.MoveAsync(Guid.NewGuid(), Staff, RequestStatus.Closed, null, CancellationToken.None);

        Assert.Equal(TransitionOutcome.Forbidden, outcome);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task Resolving_needs_a_note()
    {
        var (service, history, _) = Build(RequestStatus.InProgress);

        var outcome = await service.MoveAsync(Guid.NewGuid(), Staff, RequestStatus.Resolved, "   ", CancellationToken.None);

        Assert.Equal(TransitionOutcome.NoteRequired, outcome);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task A_valid_move_records_the_change_the_note_and_a_message_for_the_requester()
    {
        var (service, history, feedback) = Build(RequestStatus.InProgress);

        var outcome = await service.MoveAsync(
            Guid.NewGuid(), Staff, RequestStatus.Resolved, "Replaced the part.", CancellationToken.None);

        Assert.Equal(TransitionOutcome.Changed, outcome);
        Assert.Equal(new[] { "StatusChanged", "NoteAdded" }, history.Entries.Select(e => e.Action));
        var message = Assert.Single(feedback.Items);
        Assert.Equal(Requester, message.RequesterId);
        Assert.Contains("Replaced the part.", message.Message);
    }

    [Fact]
    [Trait("TestCase", "AC-007.2")]
    public async Task Rejecting_needs_a_reason()
    {
        var (service, history, feedback) = Build(RequestStatus.Received);

        var outcome = await service.RejectAsync(Guid.NewGuid(), Staff, "  ", CancellationToken.None);

        Assert.Equal(TransitionOutcome.NoteRequired, outcome);
        Assert.Empty(history.Entries);
        Assert.Empty(feedback.Items);
    }

    [Fact]
    [Trait("TestCase", "AC-007.2")]
    public async Task Rejecting_asks_for_the_reject_permission_and_tells_the_requester_why()
    {
        var policy = new FakePolicy(true);
        var (service, history, feedback) = Build(RequestStatus.Received, policy: policy);

        var outcome = await service.RejectAsync(Guid.NewGuid(), Staff, "This is a private matter.", CancellationToken.None);

        Assert.Equal(TransitionOutcome.Changed, outcome);
        Assert.Equal(RequestAction.Reject, Assert.Single(policy.Asked));
        var change = history.Entries[0];
        Assert.Equal(("Received", "Rejected"), (change.OldValue, change.NewValue));
        var message = Assert.Single(feedback.Items);
        Assert.Equal("Your request was not accepted: This is a private matter.", message.Message);
    }

    [Fact]
    public async Task Rejecting_without_the_permission_changes_nothing()
    {
        var (service, history, _) = Build(RequestStatus.Received, policy: new DenyActionPolicy(RequestAction.Reject));

        var outcome = await service.RejectAsync(Guid.NewGuid(), Staff, "Duplicate.", CancellationToken.None);

        Assert.Equal(TransitionOutcome.Forbidden, outcome);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task A_request_already_accepted_cannot_be_rejected()
    {
        var (service, history, _) = Build(RequestStatus.Assigned);

        var outcome = await service.RejectAsync(Guid.NewGuid(), Staff, "Duplicate.", CancellationToken.None);

        Assert.Equal(TransitionOutcome.NotAllowed, outcome);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task Losing_the_race_is_reported_and_nothing_is_recorded()
    {
        var (service, history, feedback) = Build(RequestStatus.Assigned, rows: 0);

        var outcome = await service.MoveAsync(Guid.NewGuid(), Staff, RequestStatus.InProgress, null, CancellationToken.None);

        Assert.Equal(TransitionOutcome.Conflicted, outcome);
        Assert.Empty(history.Entries);
        Assert.Empty(feedback.Items);
    }
}
