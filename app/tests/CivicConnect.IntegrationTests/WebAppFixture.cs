using System.Text.RegularExpressions;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Infrastructure.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace CivicConnect.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class WebAppCollection : ICollectionFixture<WebAppFixture>
{
    public const string Name = "Web application";
}

/// <summary>Display names of the development seed users (CR-003 stand-in, RSK-012).</summary>
public static class Seeded
{
    public const string RequesterOne = "Dev Requester";
    public const string RequesterTwo = DevelopmentSeed.SecondRequester;
    public const string StaffOne = "Dev Staff One";
    public const string StaffTwo = "Dev Staff Two";
}

/// <summary>
/// Hosts the whole application in process against its own throwaway database
/// (the test database name plus "_web"), so pages and API calls go through
/// routing, the policy and PostgreSQL as they do when deployed. Startup applies
/// the migrations and the development seed. Without CIVICCONNECT_TEST_DB nothing
/// starts and the tests that use this skip.
/// </summary>
public sealed class WebAppFixture : IAsyncLifetime
{
    private const string ConnectionVariable = "ConnectionStrings__CivicConnect";

    private WebApplicationFactory<Program>? factory;

    private WebApplicationFactory<Program> Factory
        => factory ?? throw new InvalidOperationException("No test database is configured.");

    public async Task InitializeAsync()
    {
        var baseConnection = Environment.GetEnvironmentVariable(RequiresDatabaseFactAttribute.Variable);
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(baseConnection);
        builder.Database += "_web";
        var connection = builder.ConnectionString;

        // Start every run from an empty database; startup migrates and seeds it.
        await using (var db = new CivicConnectDbContext(
            new DbContextOptionsBuilder<CivicConnectDbContext>().UseNpgsql(connection).Options))
        {
            await db.Database.EnsureDeletedAsync();
        }

        // Program.cs reads the connection string before the host is built, and
        // environment variables are read at that point (they also win over user secrets).
        Environment.SetEnvironmentVariable(ConnectionVariable, connection);
        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host.UseEnvironment("Development"));
        _ = factory.Server;
    }

    public Task DisposeAsync()
    {
        factory?.Dispose();
        Environment.SetEnvironmentVariable(ConnectionVariable, null);
        return Task.CompletedTask;
    }

    /// <summary>
    /// A client that behaves like one browser signed in as a seeded user: it keeps
    /// cookies (the chosen user and the antiforgery cookie) and does not follow
    /// redirects, so every response can be checked.
    /// </summary>
    public async Task<HttpClient> ClientForAsync(string displayName)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/Requests?user=" + await UserIdAsync(displayName));
        response.EnsureSuccessStatusCode();
        return client;
    }

    public Task<Guid> UserIdAsync(string displayName)
        => QueryAsync(db => db.Users.Where(u => u.DisplayName == displayName).Select(u => u.Id).SingleAsync());

    public Task<Guid> CategoryIdAsync(string name)
        => QueryAsync(db => db.Categories.Where(c => c.Name == name).Select(c => c.Id).SingleAsync());

    /// <summary>Reads the database directly, to check what a response claims.</summary>
    public async Task<T> QueryAsync<T>(Func<CivicConnectDbContext, Task<T>> query)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<CivicConnectDbContext>());
    }
}

/// <summary>Small readers for the rendered pages, kept in one place.</summary>
public static partial class Html
{
    public static string BodyOf(string html)
    {
        var start = html.IndexOf("<main>", StringComparison.Ordinal);
        var end = html.IndexOf("</main>", StringComparison.Ordinal);
        return start >= 0 && end > start ? html[start..end] : html;
    }

    public static string Token(string html) => Match(html, TokenPattern());

    /// <summary>The first status badge in the page body, as the user reads it.</summary>
    public static string Status(string html) => Match(BodyOf(html), StatusPattern());

    public static string? Message(string html)
    {
        var match = MessagePattern().Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static string OptionValue(string html, string text)
        => Match(html, new Regex("<option value=\"([^\"]+)\">" + Regex.Escape(text) + "</option>"));

    public static string Match(string html, Regex pattern)
    {
        var match = pattern.Match(html);
        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException("Pattern not found in the page: " + pattern);
    }

    public static Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string url, string token, IDictionary<string, string> fields)
    {
        var form = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token };
        return client.PostAsync(url, new FormUrlEncodedContent(form));
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();

    [GeneratedRegex("class=\"status status-[a-z]+\">([^<]+)<")]
    private static partial Regex StatusPattern();

    [GeneratedRegex("<p class=\"message\">([^<]*)</p>")]
    private static partial Regex MessagePattern();
}
