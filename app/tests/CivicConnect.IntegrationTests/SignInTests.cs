using System.Net;
using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace CivicConnect.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class SignInCollection : ICollectionFixture<SignInFixture>
{
    public const string Name = "Sign-in (outside Development)";
}

/// <summary>
/// Starts the whole application the way a deployment does: environment Staging (so
/// real cookie sign-in, not the development stand-in), an empty database of its own
/// (the test database name plus "_auth"), migrated and bootstrapped at startup from
/// configuration. Without CIVICCONNECT_TEST_DB nothing starts and the tests skip.
/// </summary>
public sealed class SignInFixture : IAsyncLifetime
{
    public const string BootstrapEmail = "admin@civicconnect.test";
    public const string BootstrapPassword = "temporary bootstrap phrase";

    private static readonly SemaphoreSlim StartupLock = new(1, 1);

    private static readonly Dictionary<string, string> Settings = new()
    {
        ["Database__MigrateOnStartup"] = "true",
        ["Bootstrap__AdminEmail"] = BootstrapEmail,
        ["Bootstrap__AdminName"] = "Bootstrap Administrator",
        ["Bootstrap__AdminPassword"] = BootstrapPassword,
        // The floor, so the tests do not spend minutes hashing. Production uses the default.
        ["Hashing__Cost"] = "100000"
    };

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
        builder.Database += "_auth";
        var connection = builder.ConnectionString;

        await using (var db = new CivicConnectDbContext(
            new DbContextOptionsBuilder<CivicConnectDbContext>().UseNpgsql(connection).Options))
        {
            await db.Database.EnsureDeletedAsync();
        }

        // Program reads configuration while it builds, and environment variables are read then.
        // They are set only for that moment and put back, so other fixtures are not affected.
        var all = new Dictionary<string, string>(Settings) { ["ConnectionStrings__CivicConnect"] = connection };
        await StartupLock.WaitAsync();
        var previous = all.Keys.ToDictionary(k => k, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var (key, value) in all)
            {
                Environment.SetEnvironmentVariable(key, value);
            }

            factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host => host.UseEnvironment("Staging"));
            _ = factory.Server;
        }
        finally
        {
            foreach (var (key, value) in previous)
            {
                Environment.SetEnvironmentVariable(key, value);
            }

            StartupLock.Release();
        }
    }

    public Task DisposeAsync()
    {
        factory?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>One browser: keeps cookies, follows no redirects, and speaks HTTPS because the cookie is Secure.</summary>
    public HttpClient NewBrowser() => Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost")
    });

    public async Task<HttpResponseMessage> SignInAsync(HttpClient browser, string email, string password, string query = "")
    {
        var page = await browser.GetStringAsync("/Account/SignIn" + query);
        return await Html.PostFormAsync(
            browser, "/Account/SignIn" + query, Html.Token(page),
            new Dictionary<string, string> { ["Email"] = email, ["Password"] = password });
    }

    /// <summary>A signed-in browser for a user who does not have to change their password.</summary>
    public async Task<(HttpClient Browser, AppUser User, string Password)> SignedInAsync(UserRole role, string? password = null)
    {
        password ??= "a long enough passphrase";
        var user = await AddUserAsync(role, password);
        var browser = NewBrowser();
        var response = await SignInAsync(browser, user.Email!, password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return (browser, user, password);
    }

    public async Task<AppUser> AddUserAsync(UserRole role, string password, bool mustChangePassword = false)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicConnectDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = AppUser.Create("Test " + role + " " + Guid.NewGuid().ToString("N")[..6], role);
        user.SetCredentials(Guid.NewGuid().ToString("N") + "@civicconnect.test", hasher.Hash(password), mustChangePassword);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<T> QueryAsync<T>(Func<CivicConnectDbContext, Task<T>> query)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<CivicConnectDbContext>());
    }

    public async Task DeactivateAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicConnectDbContext>();
        (await db.Users.SingleAsync(u => u.Id == userId)).Deactivate();
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// CR-003 (closes RSK-012): real sign-in outside Development. Everything is private
/// until someone signs in, a deactivated user or a changed password ends sessions at
/// once (AC-019.1), repeated wrong passwords lock the account, and the development
/// stand-in is not reachable.
/// </summary>
[Collection(SignInCollection.Name)]
[Trait("TestCase", "CR-003")]
public class SignInFlowTests(SignInFixture app)
{
    private static string Where(HttpResponseMessage response) => response.Headers.Location!.OriginalString;

    private static async Task<string> BodyAsync(HttpResponseMessage response) => await response.Content.ReadAsStringAsync();

    [RequiresDatabaseFact]
    public async Task Everything_is_private_until_someone_signs_in()
    {
        var browser = app.NewBrowser();

        foreach (var url in new[] { "/", "/Home", "/Requests", "/Requests/Submit", "/Staff/Queue", "/Admin/Users", "/Oversight" })
        {
            var response = await browser.GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/SignIn", Where(response));
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/v1/queue")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await browser.PostAsync("/api/v1/requests", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/Account/SignIn")).StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task The_development_user_chooser_is_not_reachable()
    {
        var browser = app.NewBrowser();
        var adminId = await app.QueryAsync(db => db.Users.Where(u => u.Role == UserRole.Administrator).Select(u => u.Id).FirstAsync());

        var chosen = await browser.GetAsync("/Requests?user=" + adminId);

        Assert.Equal(HttpStatusCode.Redirect, chosen.StatusCode);
        Assert.Contains("/Account/SignIn", Where(chosen));
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/v1/queue?user=" + adminId)).StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task A_wrong_password_and_an_unknown_email_get_the_same_answer_and_no_session()
    {
        var user = await app.AddUserAsync(UserRole.Requester, "a long enough passphrase");
        var browser = app.NewBrowser();

        var wrong = await app.SignInAsync(browser, user.Email!, "not the password at all");
        var unknown = await app.SignInAsync(browser, "nobody@civicconnect.test", "not the password at all");

        Assert.Equal(HttpStatusCode.OK, wrong.StatusCode);
        Assert.Equal("The email or password is incorrect.", Html.Message(await BodyAsync(wrong)));
        Assert.Equal(Html.Message(await BodyAsync(wrong)), Html.Message(await BodyAsync(unknown)));
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/Home")).StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task A_requester_signs_in_sees_their_pages_and_not_the_administration()
    {
        var (browser, user, _) = await app.SignedInAsync(UserRole.Requester);

        var home = await browser.GetStringAsync("/Home");
        Assert.Contains(user.DisplayName, home);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/Requests/Submit")).StatusCode);

        var users = await browser.GetStringAsync("/Admin/Users");
        Assert.Contains("User and role management is for administrators.", users);
        Assert.DoesNotContain("Create a user", users);
    }

    [RequiresDatabaseFact]
    public async Task The_session_cookie_is_secure_http_only_and_same_site()
    {
        var user = await app.AddUserAsync(UserRole.Requester, "a long enough passphrase");

        var response = await app.SignInAsync(app.NewBrowser(), user.Email!, "a long enough passphrase");

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("cc.auth=", StringComparison.Ordinal));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires", cookie, StringComparison.OrdinalIgnoreCase); // a session cookie, not a persistent one
    }

    [RequiresDatabaseFact]
    public async Task Sign_in_returns_to_a_local_page_and_never_to_another_site()
    {
        var user = await app.AddUserAsync(UserRole.Requester, "a long enough passphrase");

        var local = await app.SignInAsync(app.NewBrowser(), user.Email!, "a long enough passphrase", "?returnUrl=%2FRequests");
        var foreign = await app.SignInAsync(app.NewBrowser(), user.Email!, "a long enough passphrase", "?returnUrl=https%3A%2F%2Fevil.example%2F");

        Assert.Equal("/Requests", Where(local));
        Assert.Equal("/Home", Where(foreign));
    }

    [RequiresDatabaseFact]
    public async Task Signing_out_ends_the_session()
    {
        var (browser, _, _) = await app.SignedInAsync(UserRole.Requester);
        var home = await browser.GetStringAsync("/Home");

        var signOut = await Html.PostFormAsync(browser, "/Account/SignOut", Html.Token(home), new Dictionary<string, string>());

        Assert.Equal(HttpStatusCode.Redirect, signOut.StatusCode);
        Assert.Contains("/Account/SignIn", Where(await browser.GetAsync("/Home")));
    }

    [RequiresDatabaseFact]
    public async Task Five_wrong_passwords_lock_the_account_even_against_the_right_password()
    {
        var user = await app.AddUserAsync(UserRole.Requester, "a long enough passphrase");
        var browser = app.NewBrowser();

        for (var i = 1; i <= 4; i++)
        {
            Assert.Equal("The email or password is incorrect.", Html.Message(await BodyAsync(await app.SignInAsync(browser, user.Email!, "wrong password " + i))));
        }

        var locked = Html.Message(await BodyAsync(await app.SignInAsync(browser, user.Email!, "wrong password 5")));
        Assert.StartsWith("Too many failed attempts.", locked);

        var right = await app.SignInAsync(browser, user.Email!, "a long enough passphrase");
        Assert.Equal(HttpStatusCode.OK, right.StatusCode);
        Assert.StartsWith("Too many failed attempts.", Html.Message(await BodyAsync(right)));
    }

    /// <summary>AC-019.1: a deactivated user cannot act, and the session they already had stops at once.</summary>
    [RequiresDatabaseFact]
    public async Task A_deactivated_user_loses_the_session_at_once_and_cannot_sign_in_again()
    {
        var (browser, user, password) = await app.SignedInAsync(UserRole.ServiceStaff);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/Home")).StatusCode);

        await app.DeactivateAsync(user.Id);

        Assert.Contains("/Account/SignIn", Where(await browser.GetAsync("/Home")));
        var again = await app.SignInAsync(app.NewBrowser(), user.Email!, password);
        Assert.Equal("The email or password is incorrect.", Html.Message(await BodyAsync(again)));
    }

    [RequiresDatabaseFact]
    public async Task A_user_with_a_temporary_password_can_do_nothing_until_they_choose_their_own()
    {
        var user = await app.AddUserAsync(UserRole.Requester, "temporary passphrase", mustChangePassword: true);
        var browser = app.NewBrowser();

        var signedIn = await app.SignInAsync(browser, user.Email!, "temporary passphrase");
        Assert.Equal("/Account/ChangePassword", Where(signedIn));
        Assert.Equal("/Account/ChangePassword", Where(await browser.GetAsync("/Home")));
        Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync("/api/v1/queue")).StatusCode);

        var page = await browser.GetStringAsync("/Account/ChangePassword");
        var token = Html.Token(page);

        var mismatch = await Html.PostFormAsync(browser, "/Account/ChangePassword", token, new Dictionary<string, string>
        {
            ["CurrentPassword"] = "temporary passphrase", ["NewPassword"] = "my own new passphrase", ["ConfirmPassword"] = "something else entirely"
        });
        Assert.Equal("The new password and its confirmation do not match.", Html.Message(await BodyAsync(mismatch)));

        var weak = await Html.PostFormAsync(browser, "/Account/ChangePassword", Html.Token(await BodyAsync(mismatch)), new Dictionary<string, string>
        {
            ["CurrentPassword"] = "temporary passphrase", ["NewPassword"] = "short", ["ConfirmPassword"] = "short"
        });
        Assert.StartsWith("Use at least 12 characters", Html.Message(await BodyAsync(weak)));

        var done = await Html.PostFormAsync(browser, "/Account/ChangePassword", Html.Token(await BodyAsync(weak)), new Dictionary<string, string>
        {
            ["CurrentPassword"] = "temporary passphrase", ["NewPassword"] = "my own new passphrase", ["ConfirmPassword"] = "my own new passphrase"
        });
        Assert.Equal("/Home", Where(done));
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/Home")).StatusCode);

        // The temporary password no longer works; the new one does.
        Assert.Equal(HttpStatusCode.OK, (await app.SignInAsync(app.NewBrowser(), user.Email!, "temporary passphrase")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await app.SignInAsync(app.NewBrowser(), user.Email!, "my own new passphrase")).StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task Changing_the_password_ends_every_other_session_of_that_user()
    {
        var (first, user, password) = await app.SignedInAsync(UserRole.Requester);
        var second = app.NewBrowser();
        Assert.Equal(HttpStatusCode.Redirect, (await app.SignInAsync(second, user.Email!, password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync("/Home")).StatusCode);

        var page = await first.GetStringAsync("/Account/ChangePassword");
        var changed = await Html.PostFormAsync(first, "/Account/ChangePassword", Html.Token(page), new Dictionary<string, string>
        {
            ["CurrentPassword"] = password, ["NewPassword"] = "a different long passphrase", ["ConfirmPassword"] = "a different long passphrase"
        });

        Assert.Equal("/Home", Where(changed));
        Assert.Equal(HttpStatusCode.OK, (await first.GetAsync("/Home")).StatusCode);
        Assert.Contains("/Account/SignIn", Where(await second.GetAsync("/Home")));
    }

    /// <summary>FR-019 with CR-003: an administrator creates a user who can sign in, and the password is only ever stored hashed.</summary>
    [RequiresDatabaseFact]
    public async Task An_administrator_creates_a_user_who_signs_in_with_a_temporary_password()
    {
        var (admin, _, _) = await app.SignedInAsync(UserRole.Administrator);
        var email = Guid.NewGuid().ToString("N") + "@civicconnect.test";
        var name = "Created " + Guid.NewGuid().ToString("N")[..6];

        var page = await admin.GetStringAsync("/Admin/Users");
        var created = await Html.PostFormAsync(admin, "/Admin/Users?handler=Create", Html.Token(page), new Dictionary<string, string>
        {
            ["name"] = name, ["role"] = nameof(UserRole.Requester), ["email"] = email, ["temporaryPassword"] = "first temporary phrase"
        });
        Assert.StartsWith("User created.", Html.Message(await BodyAsync(created)));

        var stored = await app.QueryAsync(db => db.Users.AsNoTracking().SingleAsync(u => u.Email == email));
        Assert.StartsWith("v1.", stored.PasswordHash);
        Assert.DoesNotContain("first temporary phrase", stored.PasswordHash);
        Assert.True(stored.MustChangePassword);

        var newcomer = app.NewBrowser();
        Assert.Equal("/Account/ChangePassword", Where(await app.SignInAsync(newcomer, email, "first temporary phrase")));

        // The same email cannot be used twice, and a weak temporary password is refused.
        var duplicate = await Html.PostFormAsync(admin, "/Admin/Users?handler=Create", Html.Token(await BodyAsync(created)), new Dictionary<string, string>
        {
            ["name"] = "Someone Else", ["role"] = nameof(UserRole.Requester), ["email"] = email.ToUpperInvariant(), ["temporaryPassword"] = "second temporary phrase"
        });
        Assert.Equal("Another account already uses that email address.", Html.Message(await BodyAsync(duplicate)));

        var weak = await Html.PostFormAsync(admin, "/Admin/Users?handler=Create", Html.Token(await BodyAsync(duplicate)), new Dictionary<string, string>
        {
            ["name"] = "Weak One", ["role"] = nameof(UserRole.Requester), ["email"] = "weak@civicconnect.test", ["temporaryPassword"] = "short"
        });
        Assert.StartsWith("The temporary password needs at least 12 characters", Html.Message(await BodyAsync(weak)));
    }

    [RequiresDatabaseFact]
    public async Task An_administrator_reset_unlocks_the_account_and_ends_its_sessions()
    {
        var (admin, _, _) = await app.SignedInAsync(UserRole.Administrator);
        var (staff, target, _) = await app.SignedInAsync(UserRole.ServiceStaff);

        var page = await admin.GetStringAsync("/Admin/Users");
        var reset = await Html.PostFormAsync(admin, "/Admin/Users?handler=ResetPassword", Html.Token(page), new Dictionary<string, string>
        {
            ["id"] = target.Id.ToString(), ["temporaryPassword"] = "reset temporary phrase"
        });

        Assert.StartsWith("Password reset.", Html.Message(await BodyAsync(reset)));
        Assert.Contains("/Account/SignIn", Where(await staff.GetAsync("/Home")));
        Assert.Equal("/Account/ChangePassword", Where(await app.SignInAsync(app.NewBrowser(), target.Email!, "reset temporary phrase")));
    }

    [RequiresDatabaseFact]
    public async Task Only_an_administrator_can_reset_a_password()
    {
        var (requester, _, _) = await app.SignedInAsync(UserRole.Requester);
        var target = await app.AddUserAsync(UserRole.Requester, "a long enough passphrase");
        var page = await requester.GetStringAsync("/Requests/Submit");

        var response = await Html.PostFormAsync(requester, "/Admin/Users?handler=ResetPassword", Html.Token(page), new Dictionary<string, string>
        {
            ["id"] = target.Id.ToString(), ["temporaryPassword"] = "attacker chosen phrase"
        });

        Assert.Equal("Only administrators can manage users and roles.", Html.Message(await BodyAsync(response)));
        Assert.Equal(HttpStatusCode.Redirect, (await app.SignInAsync(app.NewBrowser(), target.Email!, "a long enough passphrase")).StatusCode);
    }

    /// <summary>ADR-007: a new database gets the controlled categories and one administrator, and none of the development seed.</summary>
    [RequiresDatabaseFact]
    public async Task A_new_database_is_bootstrapped_with_categories_and_one_administrator_and_no_development_data()
    {
        var categories = await app.QueryAsync(db => db.Categories.Select(c => c.Name).ToListAsync());
        Assert.Equal(7, categories.Count);
        Assert.Contains("Fault", categories);

        var admin = await app.QueryAsync(db => db.Users.AsNoTracking().SingleAsync(u => u.Email == SignInFixture.BootstrapEmail));
        Assert.Equal(UserRole.Administrator, admin.Role);
        Assert.True(admin.MustChangePassword);
        Assert.DoesNotContain(SignInFixture.BootstrapPassword, admin.PasswordHash);

        Assert.False(await app.QueryAsync(db => db.Users.AnyAsync(u => u.DisplayName.StartsWith("Dev "))));
        Assert.False(await app.QueryAsync(db => db.Requests.AnyAsync(r => r.Description == "Seeded for local development.")));

        var browser = app.NewBrowser();
        Assert.Equal("/Account/ChangePassword", Where(await app.SignInAsync(browser, SignInFixture.BootstrapEmail, SignInFixture.BootstrapPassword)));
    }
}
