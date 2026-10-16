using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// NFR-002 and AC-007.2 through the real pages. The requester's page refreshes
/// its status and updates by polling, so a staff change must be in the polled
/// fragment well inside 15 seconds; a rejection reaches the requester with its
/// reason (CR-009).
/// </summary>
[Collection(WebAppCollection.Name)]
public class UpdatesPollingTests(WebAppFixture app)
{
    private static readonly TimeSpan Target = TimeSpan.FromSeconds(15);

    private sealed record Created(Guid Id, string Reference);

    private async Task<Created> SubmitFaultAsync(HttpClient requester, string title)
    {
        var response = await requester.PostAsJsonAsync("/api/v1/requests", new
        {
            title,
            description = title + ", raised by a polling test.",
            location = "Market square",
            categoryId = await app.CategoryIdAsync("Fault")
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Created>())!;
    }

    private static Task<string> PollAsync(HttpClient requester, Guid id)
        => requester.GetStringAsync($"/Requests/Details?id={id}&handler=Updates");

    [RequiresDatabaseFact]
    [Trait("TestCase", "NFR-002")]
    public async Task A_staff_status_change_reaches_the_requesters_page_within_15_seconds()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var created = await SubmitFaultAsync(requester, "Overflowing bin");

        var page = await requester.GetStringAsync("/Requests/Details?id=" + created.Id);
        Assert.Contains($"data-status-poll=\"/Requests/Details?id={created.Id}&amp;handler=Updates\"", page);
        Assert.Contains("/js/status-poll.js", page);
        Assert.Contains("Received", Html.Status(await PollAsync(requester, created.Id)));

        var staff = await app.ClientForAsync(DevUsers.StaffOne);
        var clock = Stopwatch.StartNew();
        (await staff.PostAsync($"/api/v1/requests/{created.Id}/assignment", null)).EnsureSuccessStatusCode();
        (await staff.PostAsJsonAsync($"/api/v1/requests/{created.Id}/status", new { status = "InProgress" })).EnsureSuccessStatusCode();

        string fragment;
        do
        {
            fragment = await PollAsync(requester, created.Id);
        }
        while (!fragment.Contains("In progress") && clock.Elapsed < Target);

        clock.Stop();
        Assert.Contains("Work has started on your request.", fragment);
        Assert.True(clock.Elapsed < Target, $"The change took {clock.Elapsed.TotalSeconds:0.0} s to show.");

        // The page asks often enough to stay inside the target.
        var script = await requester.GetStringAsync("/js/status-poll.js");
        Assert.Contains("const intervalMs = 5000;", script);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "NFR-003")]
    public async Task Another_requester_cannot_poll_someone_elses_request()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var created = await SubmitFaultAsync(requester, "Blocked drain");
        var other = await app.ClientForAsync(DevUsers.RequesterTwo);

        var response = await other.GetAsync($"/Requests/Details?id={created.Id}&handler=Updates");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "AC-007.2")]
    public async Task A_rejected_request_tells_the_requester_why_and_leaves_the_queue()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var created = await SubmitFaultAsync(requester, "Neighbour's dog barking");
        var staff = await app.ClientForAsync(DevUsers.StaffOne);
        var token = Html.Token(await staff.GetStringAsync("/Staff/Details?id=" + created.Id));
        var form = new Dictionary<string, string> { ["id"] = created.Id.ToString(), ["reason"] = "  " };

        var missing = await Html.PostFormAsync(staff, $"/Staff/Details?id={created.Id}&handler=Reject", token, form);
        Assert.Equal("Rejecting a request needs a reason for the requester.", Html.Message(await missing.Content.ReadAsStringAsync()));

        form["reason"] = "Noise complaints go to the municipal by-law office.";
        var rejected = await Html.PostFormAsync(staff, $"/Staff/Details?id={created.Id}&handler=Reject", token, form);
        Assert.Equal("Request rejected. The requester has been told why.", Html.Message(await rejected.Content.ReadAsStringAsync()));

        var fragment = await PollAsync(requester, created.Id);
        Assert.Equal("Rejected", Html.Status(fragment));
        Assert.Contains("Your request was not accepted: Noise complaints go to the municipal by-law office.", fragment);

        // CR-009: nobody owns it, and it no longer waits in the queue.
        var listed = ">" + created.Reference + "</a>";
        Assert.DoesNotContain(listed, Html.BodyOf(await staff.GetStringAsync("/Staff/Queue?q=" + created.Reference)));
        Assert.Contains(listed, Html.BodyOf(await staff.GetStringAsync($"/Staff/Queue?q={created.Reference}&status=Rejected")));

        // A rejected request cannot then be accepted.
        var accept = await staff.PostAsync($"/api/v1/requests/{created.Id}/assignment", null);
        Assert.NotEqual(HttpStatusCode.OK, accept.StatusCode);
    }
}
