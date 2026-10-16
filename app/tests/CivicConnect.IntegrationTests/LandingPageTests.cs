using System.Net;
using CivicConnect.Domain.Requests;
using CivicConnect.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// The site opens on the development sign-in page, grouped by role (the CR-003
/// stand-in, RSK-012). After choosing, the home page and the navigation offer
/// each role exactly the actions the policy allows it (ADR-004), and nothing to
/// someone who is not signed in (AC-019.1).
/// </summary>
[Collection(WebAppCollection.Name)]
public class LandingPageTests(WebAppFixture app)
{
    private static readonly string[] AllActions =
        ["/Requests/Submit", "/Requests", "/Staff/Queue", "/Oversight", "/Reports/Summary", "/Admin/Users", "/Admin/Categories", "/Admin/Audit"];

    private static string Card(string href) => $"<a class=\"card\" href=\"{href}\">";

    private static string NavLink(string href) => $"<a href=\"{href}\">";

    /// <summary>Opens the site the way a browser does, following the sign-out redirect if there is one.</summary>
    private static async Task<string> OpenSiteAsync(HttpClient client)
    {
        var first = await client.GetAsync("/");
        return first.StatusCode == HttpStatusCode.Redirect
            ? await client.GetStringAsync(first.Headers.Location!.OriginalString)
            : await first.Content.ReadAsStringAsync();
    }

    private static string HeaderOf(string html)
    {
        var start = html.IndexOf("<header>", StringComparison.Ordinal);
        var end = html.IndexOf("</header>", StringComparison.Ordinal);
        return html[start..end];
    }

    [RequiresDatabaseTheory]
    [InlineData(DevUsers.RequesterOne, new[] { "/Requests/Submit", "/Requests" })]
    [InlineData(DevUsers.StaffOne, new[] { "/Staff/Queue" })]
    [InlineData("Dev Manager", new[] { "/Oversight", "/Reports/Summary" })]
    [InlineData("Dev Administrator", new[] { "/Oversight", "/Reports/Summary", "/Admin/Users", "/Admin/Categories", "/Admin/Audit" })]
    [InlineData(DevelopmentSeed.Sponsor, new[] { "/Reports/Summary" })]
    public async Task Each_role_is_offered_only_its_own_actions_on_the_home_page_and_in_the_nav(string user, string[] expected)
    {
        var client = await app.ClientForAsync(user);

        var html = await client.GetStringAsync("/Home");
        var page = Html.BodyOf(html);
        var header = HeaderOf(html);

        Assert.Contains($"<strong class=\"signed-in\">{user}</strong>", page);
        Assert.Contains($"<strong class=\"current-user\">{user}</strong>", header);
        foreach (var href in AllActions)
        {
            Assert.Equal(expected.Contains(href), page.Contains(Card(href)));
            Assert.Equal(expected.Contains(href), header.Contains(NavLink(href)));
        }
    }

    [RequiresDatabaseFact]
    public async Task The_manager_sees_the_open_and_overdue_counts_from_the_records()
    {
        var manager = await app.ClientForAsync("Dev Manager");
        var now = DateTimeOffset.UtcNow;
        var open = await app.QueryAsync(db => db.Requests.CountAsync(r =>
            r.Status == RequestStatus.Received || r.Status == RequestStatus.Assigned || r.Status == RequestStatus.InProgress));
        var overdue = await app.QueryAsync(db => db.Requests.CountAsync(r =>
            (r.Status == RequestStatus.Received || r.Status == RequestStatus.Assigned || r.Status == RequestStatus.InProgress)
            && r.TargetResponseAtUtc < now));

        var page = Html.BodyOf(await manager.GetStringAsync("/Home"));

        Assert.Contains($"{open} open, of which {overdue} ", page);
    }

    [RequiresDatabaseFact]
    public async Task Opening_the_site_always_starts_at_the_choice_even_for_a_remembered_user()
    {
        var client = await app.ClientForAsync(DevUsers.StaffOne);

        var first = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal("/", first.Headers.Location?.OriginalString);

        var html = await client.GetStringAsync("/");
        Assert.Contains("Choose who to sign in as.", Html.BodyOf(html));
        Assert.Contains("Not signed in", HeaderOf(html));
        Assert.DoesNotContain("current-user", html);
        Assert.Contains("You are not signed in.", Html.BodyOf(await client.GetStringAsync("/Home")));
    }

    [RequiresDatabaseFact]
    public async Task The_sign_in_page_groups_active_users_by_role()
    {
        var client = await app.ClientForAsync(DevUsers.RequesterOne);

        var page = Html.BodyOf(await OpenSiteAsync(client));

        foreach (var role in new[] { "Requester", "Service staff", "Management", "Administrator", "Sponsor" })
        {
            Assert.Contains($">{role}</h2>", page);
        }

        Assert.Contains(DevUsers.StaffOne, page);
        Assert.DoesNotContain(DevelopmentSeed.FormerStaff, page);

        // FR-008: staff are shown with the categories that decide what they see.
        var staffOne = page.IndexOf(DevUsers.StaffOne, StringComparison.Ordinal);
        Assert.Contains("Equipment, Fault, Other, Security", page[staffOne..]);
    }

    [RequiresDatabaseFact]
    public async Task Choosing_a_user_signs_in_as_them_and_switching_starts_again()
    {
        var client = await app.ClientForAsync(DevUsers.RequesterOne);
        var token = Html.Token(await OpenSiteAsync(client));
        var manager = await app.UserIdAsync("Dev Manager");

        var chosen = await Html.PostFormAsync(client, "/", token, new Dictionary<string, string> { ["userId"] = manager.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, chosen.StatusCode);
        Assert.Equal("/Home", chosen.Headers.Location?.OriginalString);
        Assert.Contains("<strong class=\"signed-in\">Dev Manager</strong>", await client.GetStringAsync("/Home"));

        // Switch user goes back to the site root, which leaves nobody signed in.
        await OpenSiteAsync(client);

        var html = await client.GetStringAsync("/Home");
        Assert.Contains("You are not signed in.", Html.BodyOf(html));
        Assert.Contains("Choose a user", HeaderOf(html));
        Assert.DoesNotContain("class=\"card\"", html);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-019.1")]
    public async Task A_deactivated_user_cannot_be_chosen()
    {
        var client = await app.ClientForAsync(DevUsers.RequesterOne);
        var token = Html.Token(await OpenSiteAsync(client));
        var former = await app.UserIdAsync(DevelopmentSeed.FormerStaff);

        var response = await Html.PostFormAsync(client, "/", token, new Dictionary<string, string> { ["userId"] = former.ToString() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("That user cannot sign in. Choose someone from the list.", Html.Message(await response.Content.ReadAsStringAsync()));
        Assert.Contains("You are not signed in.", Html.BodyOf(await client.GetStringAsync("/Home")));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-019.1")]
    public async Task A_user_deactivated_after_signing_in_is_offered_nothing()
    {
        var admin = await app.ClientForAsync("Dev Administrator");
        var token = Html.Token(await admin.GetStringAsync("/Admin/Users"));
        var name = "Landing " + Guid.NewGuid().ToString("N")[..6];
        await Html.PostFormAsync(admin, "/Admin/Users?handler=Create", token,
            new Dictionary<string, string> { ["name"] = name, ["role"] = "Requester" });
        var person = await app.ClientForAsync(name);
        Assert.Contains(Card("/Requests/Submit"), Html.BodyOf(await person.GetStringAsync("/Home")));

        await Html.PostFormAsync(admin, "/Admin/Users?handler=Deactivate", token,
            new Dictionary<string, string> { ["id"] = (await app.UserIdAsync(name)).ToString() });

        var page = Html.BodyOf(await person.GetStringAsync("/Home"));
        Assert.Contains("You are not signed in.", page);
        Assert.DoesNotContain("class=\"card\"", page);
    }

    [RequiresDatabaseFact]
    public async Task The_site_name_links_home_and_the_header_offers_a_switch()
    {
        var client = await app.ClientForAsync(DevUsers.RequesterOne);

        var header = HeaderOf(await client.GetStringAsync("/Requests"));

        Assert.Contains("<a href=\"/Home\" class=\"brand\">", header);
        Assert.Contains("<a href=\"/\">Switch user</a>", header);
    }
}
