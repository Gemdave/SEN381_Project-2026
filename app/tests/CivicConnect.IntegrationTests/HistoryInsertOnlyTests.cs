using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class HistoryInsertOnlyTests(TestDatabaseFixture fixture)
{
    /// <summary>ASR-03: history cannot be edited or deleted, even by someone with database access.</summary>
    [RequiresDatabaseFact]
    public async Task History_rows_cannot_be_changed_or_deleted()
    {
        var data = await TestData.CreateAsync(fixture);
        await using var db = fixture.NewContext();

        var entry = RequestHistoryEntry.ForNote(data.RequestId, data.StaffId, "first note", DateTimeOffset.UtcNow);
        db.RequestHistory.Add(entry);
        await db.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE request_history SET \"Note\" = 'edited' WHERE \"Id\" = {0}", entry.Id));
        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync(
            "DELETE FROM request_history WHERE \"Id\" = {0}", entry.Id));

        await using var check = fixture.NewContext();
        Assert.Equal("first note", check.RequestHistory.Single(h => h.Id == entry.Id).Note);
    }

    /// <summary>NFR-005: the database refuses an owner that does not match the status.</summary>
    [RequiresDatabaseFact]
    public async Task The_database_refuses_an_assigned_request_with_no_owner()
    {
        var data = await TestData.CreateAsync(fixture);
        await using var db = fixture.NewContext();

        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE requests SET \"Status\" = 'Assigned' WHERE \"Id\" = {0}", data.RequestId));
    }
}
