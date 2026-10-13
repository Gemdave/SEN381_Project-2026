using System.Net;
using System.Net.Http.Json;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// IT-01 and IT-03: the ADR-006 endpoints against the running application and
/// PostgreSQL. Each test checks the database, not just the response.
/// </summary>
[Collection(WebAppCollection.Name)]
public class RequestApiTests(WebAppFixture app)
{
    private sealed record Created(Guid Id, string Reference, string Status);

    private sealed record Validation(Dictionary<string, string[]> Errors);

    [RequiresDatabaseFact]
    [Trait("TestCase", "IT-01")]
    public async Task A_valid_request_returns_201_and_is_stored_as_Received_for_the_requester()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var requesterId = await app.UserIdAsync(DevUsers.RequesterOne);
        var fault = await app.CategoryIdAsync("Fault");

        var response = await requester.PostAsJsonAsync("/api/v1/requests", new
        {
            title = "Door will not lock",
            description = "The back door latch is broken.",
            location = "Workshop",
            categoryId = fault
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Created>();
        Assert.NotNull(created);
        Assert.StartsWith("REQ-", created.Reference);

        var stored = await app.QueryAsync(db => db.Requests.AsNoTracking().SingleAsync(r => r.Id == created.Id));
        Assert.Equal(RequestStatus.Received, stored.Status);
        Assert.Equal(requesterId, stored.RequesterId);
        Assert.Equal(created.Reference, stored.Reference);

        var history = await app.QueryAsync(db => db.RequestHistory
            .Where(h => h.RequestId == created.Id).Select(h => h.Action).ToListAsync());
        Assert.Equal(new[] { "Submitted" }, history);
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "IT-01")]
    public async Task An_empty_request_returns_400_with_an_error_for_each_field_and_stores_nothing()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var before = await app.QueryAsync(db => db.Requests.CountAsync());

        var response = await requester.PostAsJsonAsync("/api/v1/requests", new
        {
            title = "",
            description = "",
            location = "",
            categoryId = Guid.Empty
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Validation>();
        Assert.NotNull(problem);
        Assert.Equal(new[] { "categoryId", "description", "location", "title" }, problem.Errors.Keys.Order());
        Assert.Equal(before, await app.QueryAsync(db => db.Requests.CountAsync()));
    }

    [RequiresDatabaseFact]
    [Trait("TestCase", "IT-03")]
    public async Task Only_the_owning_staff_member_may_change_the_status_through_the_API()
    {
        var requester = await app.ClientForAsync(DevUsers.RequesterOne);
        var staffOne = await app.ClientForAsync(DevUsers.StaffOne);
        var staffTwo = await app.ClientForAsync(DevUsers.StaffTwo);

        // Arrange: a Fault request owned by Staff One and in progress.
        var create = await requester.PostAsJsonAsync("/api/v1/requests", new
        {
            title = "Window will not close",
            description = "The window latch in room 4 is stuck open.",
            location = "Room 4",
            categoryId = await app.CategoryIdAsync("Fault")
        });
        var id = (await create.Content.ReadFromJsonAsync<Created>())!.Id;
        Assert.Equal(HttpStatusCode.OK, (await staffOne.PostAsync($"/api/v1/requests/{id}/assignment", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staffOne.PostAsJsonAsync(
            $"/api/v1/requests/{id}/status", new { status = "InProgress" })).StatusCode);
        var historyBefore = await app.QueryAsync(db => db.RequestHistory.CountAsync(h => h.RequestId == id));

        // Act and assert: the requester and a staff member who does not own it are refused.
        foreach (var client in new[] { requester, staffTwo })
        {
            var response = await client.PostAsJsonAsync(
                $"/api/v1/requests/{id}/status", new { status = "Resolved", note = "Should be refused." });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        var stored = await app.QueryAsync(db => db.Requests.AsNoTracking().SingleAsync(r => r.Id == id));
        Assert.Equal(RequestStatus.InProgress, stored.Status);
        Assert.Equal(historyBefore, await app.QueryAsync(db => db.RequestHistory.CountAsync(h => h.RequestId == id)));
    }
}
