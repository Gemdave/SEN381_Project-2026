using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// Marks a test that needs a real PostgreSQL database. Without the CIVICCONNECT_TEST_DB
/// environment variable the test is skipped, so an ordinary `dotnet test` stays green.
/// The value is a normal connection string for a throwaway local database. See app/db for the setup.
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

/// <summary>The same rule for tests that run with several sets of data.</summary>
public sealed class RequiresDatabaseTheoryAttribute : TheoryAttribute
{
    public RequiresDatabaseTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable(RequiresDatabaseFactAttribute.Variable) is null)
        {
            Skip = "Set " + RequiresDatabaseFactAttribute.Variable + " to a throwaway PostgreSQL database to run this test.";
        }
    }
}

/// <summary>
/// One shared database for all integration tests. It is rebuilt from the real
/// migrations, so the tests check the same schema the application uses (ADR-002).
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
        await db.Database.MigrateAsync();
    }

    public CivicConnectDbContext NewContext()
        => new(new DbContextOptionsBuilder<CivicConnectDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    public Task DisposeAsync() => Task.CompletedTask;
}

/// <summary>Tests in this collection share one database and run one after another.</summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>, ICollectionFixture<ApiFactory>
{
    public const string Name = "Database";
}
