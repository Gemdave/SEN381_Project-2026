using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// Marks a test that needs a real PostgreSQL database. Without the environment
/// variable the test is skipped, so an ordinary `dotnet test` and the CI run
/// both stay green. Previously this threw, which failed the build.
///
///   setx CIVICCONNECT_TEST_DB "Host=localhost;Database=civicconnect_test;Username=postgres;Password=..."
/// </summary>
public sealed class RequiresDatabaseFactAttribute : FactAttribute
{
    public const string Variable = "CIVICCONNECT_TEST_DB";

    public RequiresDatabaseFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(Variable) is null)
        {
            Skip = "Set " + Variable + " to a throwaway PostgreSQL database to run this test.";
        }
    }
}

/// <summary>
/// The behaviour under test is the database evaluating a condition during a
/// write, so it needs a real database (ADR-002). The fixture stays quiet when
/// none is configured and the tests that use it skip.
/// </summary>
public sealed class TestDatabaseFixture : IAsyncLifetime
{
    public string? ConnectionString { get; private set; }

    public bool Available => !string.IsNullOrWhiteSpace(ConnectionString);

    public async Task InitializeAsync()
    {
        ConnectionString = Environment.GetEnvironmentVariable(RequiresDatabaseFactAttribute.Variable);
        if (!Available)
        {
            return;
        }

        await using var db = NewContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public CivicConnectDbContext NewContext()
        => new(new DbContextOptionsBuilder<CivicConnectDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    public Task DisposeAsync() => Task.CompletedTask;
}
