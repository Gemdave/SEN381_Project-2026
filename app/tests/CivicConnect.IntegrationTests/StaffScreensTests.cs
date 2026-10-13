using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// The staff screens through the real pages: the queue filters (FR-009), adding
/// a note (FR-013) and the readable history (OBS-02).
/// </summary>
[Collection(WebAppCollection.Name)]
public class StaffScreensTests(WebAppFixture app)
{
    private sealed record Created(Guid Id, string Reference);

    private async Task<Created> SubmitFaultAsync(string title)
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var response = await requester.PostAsJsonAsync("/api/v1/requests", new
        {
            title,
            description = title + ", raised by a staff screen test.",
            location = "Depot",
            categoryId = await app.CategoryIdAsync("Fault")
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Created>())!;
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-009")]
    public async Task The_queue_filters_by_status_and_category_searches_by_reference_and_says_when_nothing_matches()
    {
        var created = await SubmitFaultAsync("Broken bench");
        var staff = await app.ClientForAsync(DevUsers.StaffOne);
        var it = await app.CategoryIdAsync("IT");

        // The search box echoes the reference, so look for the result link itself.
        var listed = ">" + created.Reference + "</a>";

        Assert.Contains(listed, Html.BodyOf(await staff.GetStringAsync("/Staff/Queue?q=" + created.Reference)));
        Assert.Contains(listed, Html.BodyOf(await staff.GetStringAsync($"/Staff/Queue?q={created.Reference}&status=Received")));

        var wrongStatus = Html.BodyOf(await staff.GetStringAsync($"/Staff/Queue?q={created.Reference}&status=Closed"));
        Assert.DoesNotContain(listed, wrongStatus);
        Assert.Contains("No requests match these filters.", wrongStatus);

        var wrongCategory = Html.BodyOf(await staff.GetStringAsync($"/Staff/Queue?q={created.Reference}&categoryId={it}"));
        Assert.DoesNotContain(listed, wrongCategory);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-009")]
    public async Task The_queue_is_only_for_service_staff()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);

        var page = Html.BodyOf(await requester.GetStringAsync("/Staff/Queue"));

        Assert.Contains("The staff queue is for service staff.", page);
        Assert.DoesNotContain("request-summary", page);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "FR-013")]
    public async Task The_owner_adds_a_note_and_it_is_kept_in_the_history_with_their_name()
    {
        var created = await SubmitFaultAsync("Loose railing");
        var staff = await app.ClientForAsync(DevUsers.StaffOne);
        var staffId = await app.UserIdAsync(DevUsers.StaffOne);

        var details = await staff.GetStringAsync("/Staff/Details?id=" + created.Id);
        var token = Html.Token(details);
        await Html.PostFormAsync(staff, $"/Staff/Details?id={created.Id}&handler=Accept", token,
            new Dictionary<string, string> { ["id"] = created.Id.ToString() });

        var response = await Html.PostFormAsync(staff, $"/Staff/Details?id={created.Id}&handler=Note", token,
            new Dictionary<string, string> { ["id"] = created.Id.ToString(), ["text"] = "Tightened the brackets." });
        var page = await response.Content.ReadAsStringAsync();

        Assert.Equal("Note added.", Html.Message(page));
        Assert.Contains("Tightened the brackets.", Html.BodyOf(page));

        var stored = await app.QueryAsync(db => db.RequestHistory.AsNoTracking()
            .SingleAsync(h => h.RequestId == created.Id && h.Action == "NoteAdded"));
        Assert.Equal("Tightened the brackets.", stored.Note);
        Assert.Equal(staffId, stored.ActorUserId);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "OBS-02")]
    public async Task The_history_names_people_instead_of_showing_ids()
    {
        var created = await SubmitFaultAsync("Blocked drain");
        var staff = await app.ClientForAsync(DevUsers.StaffOne);
        var staffId = await app.UserIdAsync(DevUsers.StaffOne);
        var requesterId = await app.UserIdAsync(DevUsers.RequesterOne);

        var details = await staff.GetStringAsync("/Staff/Details?id=" + created.Id);
        var accepted = await Html.PostFormAsync(staff, $"/Staff/Details?id={created.Id}&handler=Accept", Html.Token(details),
            new Dictionary<string, string> { ["id"] = created.Id.ToString() });
        var page = await accepted.Content.ReadAsStringAsync();
        var history = page[page.IndexOf("Action history", StringComparison.Ordinal)..];

        Assert.Contains("by " + DevUsers.RequesterOne, history);
        Assert.Contains("to " + DevUsers.StaffOne, history);
        Assert.Contains("by " + DevUsers.StaffOne, history);
        Assert.DoesNotContain(staffId.ToString(), history);
        Assert.DoesNotContain(requesterId.ToString(), history);
    }
}
