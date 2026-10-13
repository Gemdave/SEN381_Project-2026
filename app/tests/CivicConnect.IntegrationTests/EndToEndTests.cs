using System.Net;
using System.Text.RegularExpressions;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// E2E-01 and E2E-02: the two critical journeys through the real pages, each
/// user in their own cookie session, against PostgreSQL. The pages are driven
/// over HTTP rather than in a browser, so page JavaScript is not exercised.
/// </summary>
[Collection(WebAppCollection.Name)]
public class EndToEndTests(WebAppFixture app)
{
    [RequiresDatabaseFact]
    [Trait("TestCase", "E2E-01")]
    public async Task A_requester_submits_a_request_and_finds_it_in_their_history_as_Received()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);

        var (_, reference, confirmation) = await SubmitAsync(requester, "Leaking tap", "Staff kitchen");

        Assert.StartsWith("REQ-", reference);
        Assert.Equal("Received", Html.Status(confirmation));
        Assert.Contains(reference, await requester.GetStringAsync("/Requests"));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "E2E-02")]
    public async Task A_request_moves_from_submission_to_closed_and_the_requester_sees_each_status()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var staff = await app.ClientForAsync(DevUsers.StaffOne);
        var (id, reference, _) = await SubmitAsync(requester, "Flickering light", "Corridor B");

        Assert.Contains(reference, await staff.GetStringAsync("/Staff/Queue"));

        var details = await staff.GetStringAsync("/Staff/Details?id=" + id);
        var token = Html.Token(details);
        var accepted = await Html.PostFormAsync(staff, $"/Staff/Details?id={id}&handler=Accept", token,
            new Dictionary<string, string> { ["id"] = id.ToString() });
        Assert.Equal("Assigned", Html.Status(await accepted.Content.ReadAsStringAsync()));
        Assert.Equal("Assigned", Html.Status(await requester.GetStringAsync("/Requests/Details?id=" + id)));

        var steps = new[]
        {
            (Next: "InProgress", Shown: "In progress", Note: ""),
            (Next: "Resolved", Shown: "Resolved", Note: "Replaced the starter."),
            (Next: "Closed", Shown: "Closed", Note: "")
        };
        foreach (var step in steps)
        {
            var moved = await Html.PostFormAsync(staff, $"/Staff/Details?id={id}&handler=Transition", token,
                new Dictionary<string, string> { ["id"] = id.ToString(), ["next"] = step.Next, ["note"] = step.Note });
            Assert.Equal(step.Shown, Html.Status(await moved.Content.ReadAsStringAsync()));
            Assert.Equal(step.Shown, Html.Status(await requester.GetStringAsync("/Requests/Details?id=" + id)));
        }

        // Every change is on record with its actor and time (AC-012.1), and the
        // resolution note is kept as its own entry (AC-014.1).
        var history = await app.QueryAsync(db => db.RequestHistory.AsNoTracking()
            .Where(h => h.RequestId == id).ToListAsync());
        Assert.Single(history, h => h.Action == "Submitted");
        Assert.Single(history, h => h.Action == "Assigned");
        Assert.Equal(3, history.Count(h => h.Action == "StatusChanged"));
        Assert.Contains(history, h => h.Action == "StatusChanged" && h.NewValue == nameof(RequestStatus.Resolved));
        Assert.Equal("Replaced the starter.", Assert.Single(history, h => h.Action == "NoteAdded").Note);
        Assert.All(history, h => Assert.NotEqual(Guid.Empty, h.ActorUserId));

        // FR-007 (AC-007.1): the requester gets a timestamped message for the
        // accept and for each status change, and the resolution says how.
        var feedback = await app.QueryAsync(db => db.Feedback.AsNoTracking()
            .Where(f => f.RequestId == id).ToListAsync());
        Assert.Equal(4, feedback.Count);
        Assert.All(feedback, f => Assert.NotEqual(default, f.CreatedAtUtc));
        Assert.Contains(feedback, f => f.Message.Contains("Replaced the starter."));
    }

    private static async Task<(Guid Id, string Reference, string Confirmation)> SubmitAsync(
        HttpClient requester, string title, string location)
    {
        var form = await requester.GetStringAsync("/Requests/Submit");
        var response = await Html.PostFormAsync(requester, "/Requests/Submit", Html.Token(form),
            new Dictionary<string, string>
            {
                ["Input.Title"] = title,
                ["Input.Description"] = title + ", reported in an end to end test.",
                ["Input.Location"] = location,
                ["Input.CategoryId"] = Html.OptionValue(form, "Fault")
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var redirect = response.Headers.Location!.ToString();
        var id = Guid.Parse(Html.Match(redirect, new Regex("id=([0-9a-f-]+)")));
        var confirmation = await requester.GetStringAsync(redirect);
        var reference = Html.Match(confirmation, new Regex("Your reference is <strong>([^<]+)</strong>"));
        return (id, reference, confirmation);
    }
}
