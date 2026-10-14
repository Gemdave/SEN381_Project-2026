using System.Net;
using System.Net.Http.Json;
using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// FR-019 (AC-019.1) and FR-020 (AC-020.1, AC-020.2) through the real Users
/// page: what an account may do follows its role and active state from its very
/// next request, and only an administrator can change either.
/// </summary>
[Collection(WebAppCollection.Name)]
public class UserAdminTests(WebAppFixture app)
{
    private const string Administrator = "Dev Administrator";
    private const string Manager = "Dev Manager";

    private sealed record Created(Guid Id, string Reference);

    private static string UniqueName(string prefix) => prefix + " " + Guid.NewGuid().ToString("N")[..6];

    private static Task<HttpResponseMessage> PostUsersAsync(HttpClient client, string handler, string token, Dictionary<string, string> fields)
        => Html.PostFormAsync(client, "/Admin/Users?handler=" + handler, token, fields);

    private async Task<(HttpClient Admin, string Token)> AdminAsync()
    {
        var admin = await app.ClientForAsync(Administrator);
        return (admin, Html.Token(await admin.GetStringAsync("/Admin/Users")));
    }

    private async Task<Guid> CreateUserAsync(string name, UserRole role)
    {
        var (admin, token) = await AdminAsync();
        var response = await PostUsersAsync(admin, "Create", token, new() { ["name"] = name, ["role"] = role.ToString() });
        Assert.Equal("User created.", Html.Message(await response.Content.ReadAsStringAsync()));
        return await app.UserIdAsync(name);
    }

    private async Task<Created> SubmitFaultAsync(string title)
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var response = await requester.PostAsJsonAsync("/api/v1/requests", new
        {
            title,
            description = title + ", raised by a user admin test.",
            location = "Town hall",
            categoryId = await app.CategoryIdAsync("Fault")
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Created>())!;
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-019.1")]
    public async Task A_deactivated_user_loses_access_at_once_and_cannot_sign_in_again()
    {
        var name = UniqueName("Temp Staff");
        var id = await CreateUserAsync(name, UserRole.ServiceStaff);
        var (admin, token) = await AdminAsync();
        await PostUsersAsync(admin, "Scopes", token, new() { ["id"] = id.ToString(), ["categoryIds"] = (await app.CategoryIdAsync("Fault")).ToString() });

        var staff = await app.ClientForAsync(name);
        Assert.Contains($"<strong>{name} (ServiceStaff)</strong>", await staff.GetStringAsync("/Staff/Queue"));

        var deactivated = await PostUsersAsync(admin, "Deactivate", token, new() { ["id"] = id.ToString() });
        Assert.StartsWith("User deactivated.", Html.Message(await deactivated.Content.ReadAsStringAsync()));

        // The browser already signed in as them can no longer act.
        var created = await SubmitFaultAsync("Fallen branch");
        var accept = await staff.PostAsync($"/api/v1/requests/{created.Id}/assignment", null);
        Assert.Equal(HttpStatusCode.Forbidden, accept.StatusCode);

        // A fresh sign-in as them does not take, and the switcher no longer offers them.
        var fresh = await app.ClientForAsync(DevUsers.RequesterOne);
        var page = await fresh.GetStringAsync("/Requests?user=" + id);
        Assert.DoesNotContain($"<strong>{name} (ServiceStaff)</strong>", page);
        Assert.DoesNotContain(">" + name + "</a>", page);

        Assert.False(await app.QueryAsync(db => db.Users.Where(u => u.Id == id).Select(u => u.IsActive).SingleAsync()));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-020.1")]
    public async Task A_role_change_changes_what_the_user_may_do_on_their_next_request()
    {
        var name = UniqueName("Promoted");
        var id = await CreateUserAsync(name, UserRole.Requester);
        var person = await app.ClientForAsync(name);

        Assert.Contains("The oversight view is for managers and administrators.", Html.BodyOf(await person.GetStringAsync("/Oversight")));

        var (admin, token) = await AdminAsync();
        var changed = await PostUsersAsync(admin, "Role", token, new() { ["id"] = id.ToString(), ["role"] = nameof(UserRole.Management) });
        Assert.StartsWith("Role changed.", Html.Message(await changed.Content.ReadAsStringAsync()));

        Assert.Contains("Total requests", Html.BodyOf(await person.GetStringAsync("/Oversight")));

        // Only requesters submit, so the same browser can no longer submit (FR-001).
        var submit = await person.PostAsJsonAsync("/api/v1/requests", new
        {
            title = "Should be refused",
            description = "A manager cannot submit.",
            location = "Nowhere",
            categoryId = await app.CategoryIdAsync("Fault")
        });
        Assert.Equal(HttpStatusCode.Forbidden, submit.StatusCode);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-020.2")]
    public async Task Someone_who_is_not_an_administrator_cannot_change_roles()
    {
        var manager = await app.ClientForAsync(Manager);
        var managerId = await app.UserIdAsync(Manager);

        var page = Html.BodyOf(await manager.GetStringAsync("/Admin/Users"));
        Assert.Contains("User and role management is for administrators.", page);
        Assert.DoesNotContain("<form", page);

        // A hand-made post, with a valid token from another page, is refused too.
        var token = Html.Token(await manager.GetStringAsync("/Requests/Submit"));
        var response = await PostUsersAsync(manager, "Role", token, new() { ["id"] = managerId.ToString(), ["role"] = nameof(UserRole.Administrator) });
        Assert.Equal("Only administrators can manage users and roles.", Html.Message(await response.Content.ReadAsStringAsync()));

        Assert.Equal(UserRole.Management, await app.QueryAsync(db => db.Users.Where(u => u.Id == managerId).Select(u => u.Role).SingleAsync()));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-020")]
    public async Task An_administrator_cannot_demote_or_deactivate_themselves()
    {
        var (admin, token) = await AdminAsync();
        var adminId = await app.UserIdAsync(Administrator);

        var response = await PostUsersAsync(admin, "Deactivate", token, new() { ["id"] = adminId.ToString() });

        Assert.Equal("You cannot change your own role or deactivate yourself.", Html.Message(await response.Content.ReadAsStringAsync()));
        Assert.True(await app.QueryAsync(db => db.Users.Where(u => u.Id == adminId).Select(u => u.IsActive).SingleAsync()));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-022")]
    public async Task User_and_role_changes_appear_in_the_audit_trail()
    {
        var name = UniqueName("Audited");
        var id = await CreateUserAsync(name, UserRole.Requester);
        var (admin, token) = await AdminAsync();
        await PostUsersAsync(admin, "Role", token, new() { ["id"] = id.ToString(), ["role"] = nameof(UserRole.Sponsor) });

        var trail = Html.BodyOf(await admin.GetStringAsync("/Admin/Audit?kind=Users"));

        Assert.Contains("User created", trail);
        Assert.Contains("Role changed", trail);
        Assert.Contains("Requester to Sponsor", trail);
        Assert.Contains(name, trail);
        Assert.Contains("by " + Administrator, trail);
        Assert.DoesNotContain("Category added", trail);
    }
}

/// <summary>
/// TC-N006 (ADR-005, NFR-006). The role matrix: for each role, every request
/// action and every page-level permission is asked of the real policy and the
/// expected allow or refuse is asserted, on a received request nobody owns yet.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class RoleMatrixTests(TestDatabaseFixture fixture)
{
    private static readonly RequestAction[] Actions = Enum.GetValues<RequestAction>();

    [RequiresDatabaseFact]
    [Trait("TestCase", "TC-N006")]
    public async Task Each_role_is_allowed_exactly_its_actions()
    {
        var data = await TestData.CreateAsync(fixture);

        Guid inactiveStaff;
        await using (var setup = fixture.NewContext())
        {
            var former = AppUser.Create("Former staff", UserRole.ServiceStaff);
            former.Deactivate();
            setup.Add(former);
            var categoryId = await setup.Requests.Where(r => r.Id == data.RequestId).Select(r => r.CategoryId).SingleAsync();
            setup.Add(StaffCategoryScope.Create(former.Id, categoryId));
            await setup.SaveChangesAsync();
            inactiveStaff = former.Id;
        }

        // View, Assign, Transition, AddNote, Close, Reject, Attach; then Submit, Oversight, Administer, Summary.
        var expected = new Dictionary<Guid, (string Role, bool[] Allowed)>
        {
            [data.RequesterId] = ("Requester", [true, false, false, false, false, false, true, true, false, false, false]),
            [data.StaffId] = ("ServiceStaff", [true, true, false, false, false, true, false, false, false, false, false]),
            [data.ManagerId] = ("Management", [true, false, false, false, false, false, false, false, true, false, true]),
            [data.AdminId] = ("Administrator", [true, false, false, false, true, false, false, false, true, true, true]),
            [data.SponsorId] = ("Sponsor", [false, false, false, false, false, false, false, false, false, false, true]),
            [inactiveStaff] = ("Deactivated staff", [false, false, false, false, false, false, false, false, false, false, false])
        };

        await using var db = fixture.NewContext();
        var policy = new AuthorisationPolicy(db);
        var ct = CancellationToken.None;

        foreach (var (userId, (role, allowed)) in expected)
        {
            var actual = new List<bool>();
            foreach (var action in Actions)
            {
                actual.Add(await policy.MayAsync(userId, action, data.RequestId, ct));
            }

            actual.Add(await policy.MaySubmitAsync(userId, ct));
            actual.Add(await policy.MayViewOversightAsync(userId, ct));
            actual.Add(await policy.MayAdministerAsync(userId, ct));
            actual.Add(await policy.MayViewSummaryAsync(userId, ct));

            Assert.True(allowed.SequenceEqual(actual), $"{role}: expected [{string.Join(", ", allowed)}] but was [{string.Join(", ", actual)}]");
        }
    }
}
