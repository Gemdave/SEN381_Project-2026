using Xunit;

namespace CivicConnect.IntegrationTests;

public sealed class TestDatabaseGuardTests
{
    [Theory]
    [InlineData("Host=localhost;Port=5433;Database=civicconnect_test;Username=postgres")]
    [InlineData("Host=127.0.0.1;Port=5432;Database=civicconnect_test;Username=postgres")]
    public void A_local_test_database_is_accepted(string connectionString)
        => TestDatabaseFixture.RefuseUnsafeTarget(connectionString);

    [Theory]
    [InlineData("Host=prod-db.example.com;Database=civicconnect_test;Username=postgres")]
    [InlineData("Host=localhost;Database=civicconnect;Username=postgres")]
    [InlineData("Host=prod-db.example.com;Database=appdb;Username=postgres")]
    public void Anything_else_is_refused_before_it_can_be_deleted(string connectionString)
        => Assert.Throws<InvalidOperationException>(
            () => TestDatabaseFixture.RefuseUnsafeTarget(connectionString));
}
