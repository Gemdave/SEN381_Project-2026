using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Infrastructure.Security;
using Xunit;

namespace CivicConnect.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class AuthorisationTests(TestDatabaseFixture fixture)
{
    /// <summary>ASR-02: a requester can open their own request and nobody else's.</summary>
    [RequiresDatabaseFact]
    public async Task A_requester_can_view_only_their_own_request()
    {
        var data = await TestData.CreateAsync(fixture);

        Guid stranger;
        await using (var setup = fixture.NewContext())
        {
            var other = AppUser.Create("Another requester", UserRole.Requester);
            setup.Add(other);
            await setup.SaveChangesAsync();
            stranger = other.Id;
        }

        await using var db = fixture.NewContext();
        var policy = new AuthorisationPolicy(db);
        var ct = CancellationToken.None;

        Assert.True(await policy.MayAsync(data.RequesterId, RequestAction.View, data.RequestId, ct));
        Assert.False(await policy.MayAsync(stranger, RequestAction.View, data.RequestId, ct));
    }

    /// <summary>ASR-02: a requester can never use staff actions, even on their own request.</summary>
    [RequiresDatabaseFact]
    public async Task A_requester_cannot_use_staff_actions()
    {
        var data = await TestData.CreateAsync(fixture);
        await using var db = fixture.NewContext();
        var policy = new AuthorisationPolicy(db);

        foreach (var action in new[] { RequestAction.Assign, RequestAction.Transition, RequestAction.AddNote, RequestAction.Close })
        {
            Assert.False(await policy.MayAsync(data.RequesterId, action, data.RequestId, CancellationToken.None), action + " should be refused");
        }
    }
}
