using System.Diagnostics;
using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Domain.Requests;
using CivicConnect.Infrastructure.Persistence.Repositories;
using Xunit;

namespace CivicConnect.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class QueueTests(TestDatabaseFixture fixture)
{
    [RequiresDatabaseFact]
    public async Task Staff_see_unassigned_and_their_own_requests_but_not_other_peoples()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        Guid open, mine, theirs, staffA;

        await using (var setup = fixture.NewContext())
        {
            var category = Category.Create("Queue " + token);
            var requester = AppUser.Create("Requester", UserRole.Requester);
            var a = AppUser.Create("Staff A", UserRole.ServiceStaff);
            var b = AppUser.Create("Staff B", UserRole.ServiceStaff);
            var now = DateTimeOffset.UtcNow;

            var r1 = Request.Submit("Q1-" + token, "Pothole open " + token, "x", category.Id, requester.Id, now);
            var r2 = Request.Submit("Q2-" + token, "Pothole mine " + token, "x", category.Id, requester.Id, now);
            var r3 = Request.Submit("Q3-" + token, "Pothole theirs " + token, "x", category.Id, requester.Id, now);
            setup.AddRange(category, requester, a, b, r1, r2, r3);
            await setup.SaveChangesAsync();

            var repository = new RequestRepository(setup);
            await repository.TryAssignAsync(r2.Id, a.Id, now, CancellationToken.None);
            await repository.TryAssignAsync(r3.Id, b.Id, now, CancellationToken.None);

            open = r1.Id;
            mine = r2.Id;
            theirs = r3.Id;
            staffA = a.Id;
        }

        await using var db = fixture.NewContext();
        var queue = new QueueRepository(db);
        var ct = CancellationToken.None;

        var visible = (await queue.SearchAsync(staffA, new QueueFilter(Search: token), ct)).Select(r => r.Id).ToList();
        Assert.Contains(open, visible);
        Assert.Contains(mine, visible);
        Assert.DoesNotContain(theirs, visible);

        var onlyMine = (await queue.SearchAsync(staffA, new QueueFilter(Search: token, MineOnly: true), ct)).Select(r => r.Id);
        Assert.Equal(new[] { mine }, onlyMine);

        var assigned = (await queue.SearchAsync(staffA, new QueueFilter(Search: token, Status: RequestStatus.Assigned), ct)).Select(r => r.Id);
        Assert.Equal(new[] { mine }, assigned);

        var newestFirst = (await queue.SearchAsync(staffA, new QueueFilter(Search: token, Sort: QueueSort.Reference), ct)).Select(r => r.Id);
        Assert.Equal(new[] { open, mine }, newestFirst);
    }

    /// <summary>NFR-004: the queue loads within 3 seconds at 1,000 requests.</summary>
    [RequiresDatabaseFact]
    public async Task The_queue_loads_in_under_three_seconds_with_1000_requests()
    {
        var token = Guid.NewGuid().ToString("N")[..8];
        Guid staffId;

        await using (var setup = fixture.NewContext())
        {
            var category = Category.Create("Perf " + token);
            var requester = AppUser.Create("Requester", UserRole.Requester);
            var staff = AppUser.Create("Staff", UserRole.ServiceStaff);
            setup.AddRange(category, requester, staff);

            for (var i = 0; i < 1000; i++)
            {
                setup.Requests.Add(Request.Submit(
                    "PERF-" + token + "-" + i.ToString("D4"),
                    "Perf request " + i,
                    "Volume test.",
                    category.Id,
                    requester.Id,
                    DateTimeOffset.UtcNow.AddSeconds(-i)));
            }

            await setup.SaveChangesAsync();
            staffId = staff.Id;
        }

        await using var db = fixture.NewContext();
        var queue = new QueueRepository(db);

        await queue.SearchAsync(staffId, new QueueFilter(), CancellationToken.None);

        var watch = Stopwatch.StartNew();
        var rows = await queue.SearchAsync(staffId, new QueueFilter(), CancellationToken.None);
        watch.Stop();

        Assert.NotEmpty(rows);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), "The queue took " + watch.Elapsed.TotalMilliseconds + " ms.");
    }
}
