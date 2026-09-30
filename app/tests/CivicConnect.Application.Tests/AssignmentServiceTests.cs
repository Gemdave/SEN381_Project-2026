using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.Application.Tests;

public class AssignmentServiceTests
{
    private static AssignmentService Build(
        int rowsAffected,
        RequestOwnership? ownership,
        bool permitted,
        FakeHistoryRepository history)
        => new(
            new FakeRequestRepository(rowsAffected, ownership),
            history,
            new FakePolicy(permitted),
            new DirectUnitOfWork(),
            new FixedClock());

    [Fact]
    public async Task One_affected_row_means_the_accept_succeeded_and_history_is_written()
    {
        var history = new FakeHistoryRepository();
        var service = Build(rowsAffected: 1, ownership: null, permitted: true, history);

        var result = await service.AcceptAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(AssignmentOutcome.Assigned, result.Outcome);
        Assert.Single(history.Entries);
    }

    [Fact]
    public async Task Zero_affected_rows_with_an_owner_is_a_conflict_and_writes_no_history()
    {
        var owner = Guid.NewGuid();
        var history = new FakeHistoryRepository();
        var ownership = new RequestOwnership(Guid.NewGuid(), owner, RequestStatus.Assigned);
        var service = Build(rowsAffected: 0, ownership, permitted: true, history);

        var result = await service.AcceptAsync(ownership.RequestId, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(AssignmentOutcome.AlreadyOwned, result.Outcome);
        Assert.Equal(owner, result.OwnerId);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task A_missing_request_is_reported_as_not_found()
    {
        var service = Build(rowsAffected: 0, ownership: null, permitted: true, new FakeHistoryRepository());

        var result = await service.AcceptAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(AssignmentOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task The_policy_is_checked_before_anything_is_written()
    {
        var history = new FakeHistoryRepository();
        var service = Build(rowsAffected: 1, ownership: null, permitted: false, history);

        var result = await service.AcceptAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(AssignmentOutcome.Forbidden, result.Outcome);
        Assert.Empty(history.Entries);
    }
}
