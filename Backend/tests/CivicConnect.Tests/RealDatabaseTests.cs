using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;
using CivicConnect.Core.Services;
using CivicConnect.Data;
using CivicConnect.Tests.Support;
using Npgsql;

namespace CivicConnect.Tests;

// These run against a real PostgreSQL database. Point CIVICCONNECT_TEST_DB at a
// throwaway database (never a shared one - history rows cannot be deleted, by design).
//   e.g.  Host=localhost;Port=5432;Database=civicconnect_test;Username=postgres;Password=...
// Without that variable they are skipped, so a normal `dotnet test` still passes.
public sealed class RequiresDatabaseFactAttribute : FactAttribute
{
    public const string Variable = "CIVICCONNECT_TEST_DB";

    public RequiresDatabaseFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(Variable) is null)
            Skip = $"Set {Variable} to a throwaway PostgreSQL database to run this test.";
    }
}

public class RealDatabaseTests
{
    private static async Task<NpgsqlDataSource> ConnectAsync()
    {
        var ds = NpgsqlDataSource.Create(Environment.GetEnvironmentVariable(RequiresDatabaseFactAttribute.Variable)!);
        await MigrationRunner.RunAsync(ds);
        return ds;
    }

    private static async Task<CurrentUser> NewUserAsync(NpgsqlDataSource ds, string prefix, IEnumerable<string> permissions)
    {
        var id = Guid.NewGuid();
        await using var cmd = ds.CreateCommand(
            "INSERT INTO app_user (id, email, display_name) VALUES (@id, @email, @name)");
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("email", $"{prefix}-{id:N}@test.local");
        cmd.Parameters.AddWithValue("name", prefix);
        await cmd.ExecuteNonQueryAsync();
        return new CurrentUser(id, prefix, permissions.ToHashSet());
    }

    [RequiresDatabaseFact]
    [Trait("TC", "TC-011.2")]
    public async Task Ten_staff_accepting_at_once_leaves_exactly_one_owner()
    {
        await using var ds = await ConnectAsync();
        var factory = new NpgsqlUnitOfWorkFactory(ds);
        var policy = new AuthorisationPolicy();
        var requests = new RequestService(factory, policy);
        var assignments = new AssignmentService(factory, policy);
        var feedback = new FeedbackService(factory, policy);
        var categories = new CategoryService(factory);

        var requester = await NewUserAsync(ds, "requester", TestUsers.RequesterPermissions);
        var staff = new List<CurrentUser>();
        for (var i = 0; i < 10; i++)
            staff.Add(await NewUserAsync(ds, $"staff{i}", TestUsers.StaffPermissions));

        var category = (await categories.ListActiveAsync()).First();
        var request = await requests.SubmitAsync(requester,
            new NewRequest("Concurrency check", "Ten people press accept.", "Test lab", category.Id));

        var outcomes = await Task.WhenAll(staff.Select(s => Task.Run(async () =>
        {
            try { await assignments.AcceptAsync(s, request.Id); return (winner: s, ok: true); }
            catch (ConflictException) { return (winner: s, ok: false); }
        })));

        var winners = outcomes.Where(o => o.ok).ToList();
        Assert.Single(winners);

        var detail = await requests.GetAsync(winners[0].winner, request.Id);
        Assert.Equal(winners[0].winner.Id, detail.Request.AssignedTo);
        Assert.Single(detail.History!, h => h.Action == "Assigned");
        Assert.Single(await feedback.ListAsync(requester), f => f.Kind == FeedbackKinds.Accepted);
    }

    [RequiresDatabaseFact]
    [Trait("TC", "TC-N007")]
    public async Task History_rows_cannot_be_changed_or_deleted()
    {
        await using var ds = await ConnectAsync();
        var factory = new NpgsqlUnitOfWorkFactory(ds);
        var requests = new RequestService(factory, new AuthorisationPolicy());
        var categories = new CategoryService(factory);

        var requester = await NewUserAsync(ds, "requester", TestUsers.RequesterPermissions);
        var category = (await categories.ListActiveAsync()).First();
        var request = await requests.SubmitAsync(requester,
            new NewRequest("Tamper check", "Try to edit history.", "Test lab", category.Id));

        await using var update = ds.CreateCommand(
            "UPDATE request_status_history SET note = 'edited' WHERE request_id = @id");
        update.Parameters.AddWithValue("id", request.Id);
        await Assert.ThrowsAsync<PostgresException>(() => update.ExecuteNonQueryAsync());

        await using var delete = ds.CreateCommand("DELETE FROM request_status_history WHERE request_id = @id");
        delete.Parameters.AddWithValue("id", request.Id);
        await Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());
    }

    [RequiresDatabaseFact]
    [Trait("TC", "TC-N005")]
    public async Task The_database_itself_refuses_an_owner_on_a_Received_request()
    {
        await using var ds = await ConnectAsync();
        var factory = new NpgsqlUnitOfWorkFactory(ds);
        var requests = new RequestService(factory, new AuthorisationPolicy());
        var categories = new CategoryService(factory);

        var requester = await NewUserAsync(ds, "requester", TestUsers.RequesterPermissions);
        var category = (await categories.ListActiveAsync()).First();
        var request = await requests.SubmitAsync(requester,
            new NewRequest("Constraint check", "Bypass the service.", "Test lab", category.Id));

        // Going around the backend on purpose: the CHECK constraint should still stop it.
        await using var cmd = ds.CreateCommand(
            "UPDATE request SET assigned_to = @user, assigned_at = now() WHERE id = @id");
        cmd.Parameters.AddWithValue("user", requester.Id);
        cmd.Parameters.AddWithValue("id", request.Id);
        await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
    }
}
