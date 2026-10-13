using CivicConnect.Domain.Requests;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Infrastructure.Persistence.Repositories;
using Xunit;

namespace CivicConnect.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class TransactionTests(TestDatabaseFixture fixture)
{
    /// <summary>RSK-009: a failure part-way through must not leave a half-saved change.</summary>
    [RequiresDatabaseFact]
    public async Task A_failure_after_the_ownership_change_rolls_everything_back()
    {
        var data = await TestData.CreateAsync(fixture);

        await using (var db = fixture.NewContext())
        {
            var requests = new RequestRepository(db);
            var history = new RequestHistoryRepository(db);
            var unitOfWork = new UnitOfWork(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync<int>(async token =>
            {
                await requests.TryAssignAsync(data.RequestId, data.StaffId, DateTimeOffset.UtcNow, token);
                history.Add(RequestHistoryEntry.ForAssignment(data.RequestId, data.StaffId, DateTimeOffset.UtcNow));
                await unitOfWork.SaveChangesAsync(token);
                throw new InvalidOperationException("simulated failure");
            }, CancellationToken.None));
        }

        await using var check = fixture.NewContext();
        var stored = check.Requests.Single(r => r.Id == data.RequestId);
        Assert.Null(stored.AssignedToUserId);
        Assert.Equal(RequestStatus.Received, stored.Status);
        Assert.Empty(check.RequestHistory.Where(h => h.RequestId == data.RequestId));
    }
}
