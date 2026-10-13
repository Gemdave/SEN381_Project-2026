using System.Net;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// BB-05: FR-004, AC-004.2 and NFR-003 between two real requesters, through
/// the pages a requester uses.
/// </summary>
[Collection(WebAppCollection.Name)]
public class RequesterPrivacyTests(WebAppFixture app)
{
    [RequiresDatabaseFact]
    [Trait("TestCase", "BB-05")]
    public async Task A_requester_never_sees_another_requesters_requests()
    {
        var one = await app.ClientForAsync(Seeded.RequesterOne);
        var two = await app.ClientForAsync(Seeded.RequesterTwo);
        var twoId = await app.UserIdAsync(Seeded.RequesterTwo);
        var theirs = await app.QueryAsync(db => db.Requests
            .Where(r => r.RequesterId == twoId)
            .Select(r => new { r.Id, r.Reference })
            .ToListAsync());
        Assert.Equal(2, theirs.Count);

        // Not in Requester One's history.
        var history = await one.GetStringAsync("/Requests");
        foreach (var request in theirs)
        {
            Assert.DoesNotContain(request.Reference, history);
        }

        // Not reachable by ID either: it answers exactly like a missing request.
        foreach (var request in theirs)
        {
            var response = await one.GetAsync("/Requests/Details?id=" + request.Id);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain(request.Reference, await response.Content.ReadAsStringAsync());
        }

        // Control: the owner does see them.
        var own = await two.GetStringAsync("/Requests");
        foreach (var request in theirs)
        {
            Assert.Contains(request.Reference, own);
        }
    }
}
