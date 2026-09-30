using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// These tests need a real PostgreSQL database, because the behaviour under test
/// is the database evaluating a condition during a write. A mocked repository
/// cannot prove it (ADR-002).
///
/// Set CIVICCONNECT_TEST_DB before running, for example:
///   setx CIVICCONNECT_TEST_DB "Host=localhost;Database=civicconnect_test;Username=postgres;Password=..."
/// </summary>
public sealed class TestDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        ConnectionString = Environment.GetEnvironmentVariable("CIVICCONNECT_TEST_DB")
            ?? throw new InvalidOperationException(
                "CIVICCONNECT_TEST_DB is not set. These tests need a real PostgreSQL database.");

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
