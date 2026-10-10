using CivicConnect.Application.Results;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Domain.Requests;
using CivicConnect.Application.Requests;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Infrastructure.Persistence.Repositories;
using CivicConnect.Infrastructure.Security;
using Xunit;

namespace CivicConnect.IntegrationTests;

/// <summary>
/// TC-011.2, the verification evidence for the M2 demonstration trace:
/// FR-011, ASR-01, NFR-005, AC-011.2. Skipped when no test database is set.
/// </summary>
public class ConcurrentAcceptTests(TestDatabaseFixture fixture) : IClassFixture<TestDatabaseFixture>
{
    [RequiresDatabaseFact]
    public async Task Exactly_one_of_eight_simultaneous_accepts_succeeds()
    {
        const int attempts = 8;

        Guid requestId;
        var staffIds = new List<Guid>();

        await using (var setup = fixture.NewContext())
        {
            var category = Category.Create("Fault " + Guid.NewGuid().ToString("N")[..6]);
            setup.Categories.Add(category);

            var requester = AppUser.Create("Requester", UserRole.Requester);
            setup.Users.Add(requester);

            for (var i = 0; i < attempts; i++)
            {
                var staff = AppUser.Create("Staff " + i, UserRole.ServiceStaff);
                setup.Users.Add(staff);
                staffIds.Add(staff.Id);
            }

            var request = Request.Submit(
                reference: "REQ-" + Random.Shared.Next(1000, 9999),
                title: "Concurrent accept",
                description: "Two staff accept at the same moment.",
                categoryId: category.Id,
                requesterId: requester.Id,
                now: DateTimeOffset.UtcNow);

            setup.Requests.Add(request);
            await setup.SaveChangesAsync();
            requestId = request.Id;
        }

        // Each attempt gets its own context and transaction, which is what makes
        // this a real race rather than a sequence.
        var results = await Task.WhenAll(staffIds.Select(async staffId =>
        {
            await using var db = fixture.NewContext();
            var service = new AssignmentService(
                new RequestRepository(db),
                new RequestHistoryRepository(db),
                new AuthorisationPolicy(db),
                new UnitOfWork(db),
                new SystemClock());

            return await service.AcceptAsync(requestId, staffId, CancellationToken.None);
        }));

        Assert.Equal(1, results.Count(r => r.Outcome == AssignmentOutcome.Assigned));
        Assert.Equal(attempts - 1, results.Count(r => r.Outcome == AssignmentOutcome.AlreadyOwned));

        await using var check = fixture.NewContext();
        var stored = check.Requests.Single(r => r.Id == requestId);
        Assert.NotNull(stored.AssignedToUserId);
        Assert.Equal(RequestStatus.Assigned, stored.Status);

        var history = check.RequestHistory.Where(h => h.RequestId == requestId).ToList();
        Assert.Single(history);
        Assert.Equal("Assigned", history[0].Action);
        Assert.Equal(stored.AssignedToUserId, history[0].ActorUserId);
    }
}
