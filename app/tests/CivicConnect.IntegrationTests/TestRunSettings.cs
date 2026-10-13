using Xunit;

// The database tests start the application in process (ApiFactory and
// WebAppFixture). Program.cs reads its connection string from a process wide
// environment variable, so collections run one at a time to keep each host on
// its own database.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
