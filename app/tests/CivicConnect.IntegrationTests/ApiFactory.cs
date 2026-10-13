using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CivicConnect.IntegrationTests;

/// <summary>The real application, started in memory and pointed at the test database.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        // Program reads the connection string while it builds, so this has to be set first.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__CivicConnect",
            Environment.GetEnvironmentVariable(RequiresDatabaseFactAttribute.Variable));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");
}
