using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// Calls the real API over HTTP against the real database (ADR-006).
/// The "user" query value picks who is acting, because sign-in is a development stand-in (CR-003).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ApiTests(TestDatabaseFixture fixture, ApiFactory factory)
{
    private static string Url(Guid requestId, string action, Guid userId)
        => "/api/v1/requests/" + requestId + "/" + action + "?user=" + userId;

    private async Task<HttpResponseMessage> AcceptAsync(Guid requestId, Guid userId)
        => await factory.CreateClient().PostAsync(Url(requestId, "assignment", userId), null);

    private async Task<HttpResponseMessage> MoveAsync(Guid requestId, Guid userId, string status, string? note = null)
        => await factory.CreateClient().PostAsJsonAsync(Url(requestId, "status", userId), new { status, note });

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("code").GetString();
    }

    private static async Task<List<Guid>> QueueIdsAsync(HttpClient client, string query)
    {
        var rows = await client.GetFromJsonAsync<List<JsonElement>>("/api/v1/queue" + query);
        return rows!.Select(r => r.GetProperty("id").GetGuid()).ToList();
    }

    /// <summary>FR-014 / AC-014.1: resolving needs a note (empty versus present).</summary>
    [RequiresDatabaseFact]
    public async Task Resolving_without_a_note_is_refused_and_with_a_note_is_accepted()
    {
        var data = await TestData.CreateAsync(fixture);
        Assert.Equal(HttpStatusCode.OK, (await AcceptAsync(data.RequestId, data.StaffId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(data.RequestId, data.StaffId, "InProgress")).StatusCode);

        var withoutNote = await MoveAsync(data.RequestId, data.StaffId, "Resolved");
        Assert.Equal(HttpStatusCode.BadRequest, withoutNote.StatusCode);
        Assert.Equal("note_required", await CodeAsync(withoutNote));

        var withNote = await MoveAsync(data.RequestId, data.StaffId, "Resolved", "Replaced the part.");
        Assert.Equal(HttpStatusCode.OK, withNote.StatusCode);
    }

    /// <summary>FR-012 / AC-012.2, state transition: from Assigned, only the next step is allowed.</summary>
    [RequiresDatabaseTheory]
    [InlineData("Received", HttpStatusCode.Conflict)]
    [InlineData("Assigned", HttpStatusCode.Conflict)]
    [InlineData("Closed", HttpStatusCode.Conflict)]
    [InlineData("Banana", HttpStatusCode.BadRequest)]
    public async Task From_assigned_the_wrong_next_status_is_refused(string target, HttpStatusCode expected)
    {
        var data = await TestData.CreateAsync(fixture);
        await AcceptAsync(data.RequestId, data.StaffId);

        var response = await MoveAsync(data.RequestId, data.StaffId, target);

        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>ASR-02, negative: only the owner can change status, and requesters never can.</summary>
    [RequiresDatabaseFact]
    public async Task Only_the_owner_can_change_status_and_requesters_cannot()
    {
        var data = await TestData.CreateAsync(fixture);
        await AcceptAsync(data.RequestId, data.StaffId);

        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(data.RequestId, data.OtherStaffId, "InProgress")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(data.RequestId, data.RequesterId, "InProgress")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await AcceptAsync(data.RequestId, data.RequesterId)).StatusCode);
    }

    /// <summary>FR-013, boundary values: a note is 1 to 2000 characters.</summary>
    [RequiresDatabaseTheory]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(2000, HttpStatusCode.OK)]
    [InlineData(2001, HttpStatusCode.BadRequest)]
    public async Task A_note_must_be_between_1_and_2000_characters(int length, HttpStatusCode expected)
    {
        var data = await TestData.CreateAsync(fixture);
        await AcceptAsync(data.RequestId, data.StaffId);

        var response = await factory.CreateClient()
            .PostAsJsonAsync(Url(data.RequestId, "notes", data.StaffId), new { text = new string('a', length) });

        Assert.Equal(expected, response.StatusCode);
    }

    /// <summary>FR-008 / FR-009: the queue shows unassigned and own requests, filters work, requesters are refused.</summary>
    [RequiresDatabaseFact]
    public async Task The_queue_api_scopes_filters_and_refuses_non_staff()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        var mine = await TestData.CreateAsync(fixture, "Streetlight mine " + token);
        var theirs = await TestData.CreateAsync(fixture, "Streetlight theirs " + token);
        var open = await TestData.CreateAsync(fixture, "Streetlight open " + token);
        await AcceptAsync(mine.RequestId, mine.StaffId);
        await AcceptAsync(theirs.RequestId, theirs.StaffId);

        var client = factory.CreateClient();

        var visible = await QueueIdsAsync(client, "?q=" + token + "&user=" + mine.StaffId);
        Assert.Contains(mine.RequestId, visible);
        Assert.Contains(open.RequestId, visible);
        Assert.DoesNotContain(theirs.RequestId, visible);

        var onlyMine = await QueueIdsAsync(client, "?q=" + token + "&mine=true&user=" + mine.StaffId);
        Assert.Equal(new[] { mine.RequestId }, onlyMine);

        var asRequester = await client.GetAsync("/api/v1/queue?user=" + mine.RequesterId);
        Assert.Equal(HttpStatusCode.Forbidden, asRequester.StatusCode);
    }

    /// <summary>E2E: the main journey, accept to close, through the API with history and messages checked in the database.</summary>
    [RequiresDatabaseFact]
    public async Task E2E_a_request_is_accepted_worked_resolved_and_closed_through_the_api()
    {
        var data = await TestData.CreateAsync(fixture);

        Assert.Equal(HttpStatusCode.OK, (await AcceptAsync(data.RequestId, data.StaffId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(data.RequestId, data.StaffId, "InProgress")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(data.RequestId, data.StaffId, "Resolved", "Fixed the fault.")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(data.RequestId, data.OtherStaffId, "Closed")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(data.RequestId, data.AdminId, "Closed")).StatusCode);

        await using var db = fixture.NewContext();
        Assert.Equal(RequestStatus.Closed, db.Requests.Single(r => r.Id == data.RequestId).Status);
        Assert.Equal(5, db.RequestHistory.Count(h => h.RequestId == data.RequestId));
        Assert.Equal(4, db.Feedback.Count(f => f.RequestId == data.RequestId));
    }

    /// <summary>E2E, ASR-01: eight staff accept the same request at once over HTTP and exactly one wins.</summary>
    [RequiresDatabaseFact]
    public async Task E2E_eight_staff_accepting_at_once_produce_exactly_one_owner()
    {
        var data = await TestData.CreateAsync(fixture);
        var staffIds = new List<Guid>();

        await using (var setup = fixture.NewContext())
        {
            for (var i = 0; i < 8; i++)
            {
                var staff = AppUser.Create("Racing staff " + i, UserRole.ServiceStaff);
                setup.Add(staff);
                staffIds.Add(staff.Id);
            }

            await setup.SaveChangesAsync();
        }

        var responses = await Task.WhenAll(staffIds.Select(id => AcceptAsync(data.RequestId, id)));
        var codes = responses.Select(r => r.StatusCode).ToList();

        Assert.Equal(1, codes.Count(c => c == HttpStatusCode.OK));
        Assert.Equal(7, codes.Count(c => c == HttpStatusCode.Conflict));

        await using var db = fixture.NewContext();
        Assert.Equal(1, db.RequestHistory.Count(h => h.RequestId == data.RequestId && h.Action == "Assigned"));
    }
}
