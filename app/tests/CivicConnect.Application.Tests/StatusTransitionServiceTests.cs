using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.Application.Tests;

/// <summary>
/// FR-012 and FR-014 at component level (AC-012.1, AC-012.2, AC-014.1, AC-014.2).
/// </summary>
public class StatusTransitionServiceTests
{
    private static StatusTransitionService Build(
        RequestStatus current, FakePolicy policy, FakeHistoryRepository history, int rowsAffected = 1)
        => new(
            new FakeRequestRepository(rowsAffected, new RequestOwnership(Guid.NewGuid(), Guid.NewGuid(), current)),
            history,
            policy,
            new DirectUnitOfWork(),
            new FixedClock());

    [Fact]
    [Trait("TestCase", "UT-04")]
    public async Task Closing_is_checked_as_its_own_action()
    {
        var policy = new FakePolicy(permitted: true);
        var service = Build(RequestStatus.Resolved, policy, new FakeHistoryRepository());

        await service.MoveAsync(Guid.NewGuid(), Guid.NewGuid(), RequestStatus.Closed, null, CancellationToken.None);

        Assert.Equal(new[] { RequestAction.Close }, policy.Asked);
    }

    [Fact]
    [Trait("TestCase", "UT-04")]
    public async Task A_refused_close_changes_nothing()
    {
        var history = new FakeHistoryRepository();
        var service = Build(RequestStatus.Resolved, new FakePolicy(permitted: false), history);

        var outcome = await service.MoveAsync(Guid.NewGuid(), Guid.NewGuid(), RequestStatus.Closed, null, CancellationToken.None);

        Assert.Equal(TransitionOutcome.Forbidden, outcome);
        Assert.Empty(history.Entries);
    }

    [Fact]
    [Trait("TestCase", "UT-01")]
    public async Task An_invalid_jump_is_refused_and_writes_no_history()
    {
        var history = new FakeHistoryRepository();
        var service = Build(RequestStatus.Received, new FakePolicy(permitted: true), history);

        var outcome = await service.MoveAsync(Guid.NewGuid(), Guid.NewGuid(), RequestStatus.Closed, null, CancellationToken.None);

        Assert.Equal(TransitionOutcome.NotAllowed, outcome);
        Assert.Empty(history.Entries);
    }

    [Fact]
    [Trait("TestCase", "UT-01")]
    public async Task A_resolution_note_is_recorded_with_the_status_change()
    {
        var history = new FakeHistoryRepository();
        var service = Build(RequestStatus.InProgress, new FakePolicy(permitted: true), history);

        var outcome = await service.MoveAsync(
            Guid.NewGuid(), Guid.NewGuid(), RequestStatus.Resolved, "  Replaced the bulb.  ", CancellationToken.None);

        Assert.Equal(TransitionOutcome.Changed, outcome);
        var entry = Assert.Single(history.Entries);
        Assert.Equal(nameof(RequestStatus.Resolved), entry.NewValue);
        Assert.Equal("Replaced the bulb.", entry.Note);
    }

    [Fact]
    public async Task An_overlong_note_is_refused_before_anything_is_written()
    {
        var history = new FakeHistoryRepository();
        var service = Build(RequestStatus.InProgress, new FakePolicy(permitted: true), history);

        var outcome = await service.MoveAsync(
            Guid.NewGuid(), Guid.NewGuid(), RequestStatus.Resolved,
            new string('a', StatusTransitionService.NoteMax + 1), CancellationToken.None);

        Assert.Equal(TransitionOutcome.NoteTooLong, outcome);
        Assert.Empty(history.Entries);
    }
}
