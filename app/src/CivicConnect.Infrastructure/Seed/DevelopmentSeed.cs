using CivicConnect.Domain.Access;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Domain.Requests;
using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Seed;

/// <summary>
/// Development only. This is the CR-003 contingency recorded against RSK-012:
/// seeded users stand in for authentication until that decision closes. It must
/// never run outside the Development environment.
/// </summary>
public static class DevelopmentSeed
{
    public static async Task RunAsync(CivicConnectDbContext db, CancellationToken ct = default)
    {
        if (await db.Categories.AnyAsync(ct))
        {
            return;
        }

        // FR-002. The final list is pending CR-008.
        string[] names = ["Fault", "Equipment", "Security", "IT", "Maintenance", "Lost Property", "Other"];
        var categories = names.Select(Category.Create).ToList();
        db.Categories.AddRange(categories);

        var requester = AppUser.Create("Dev Requester", UserRole.Requester);
        var staffOne = AppUser.Create("Dev Staff One", UserRole.ServiceStaff);
        var staffTwo = AppUser.Create("Dev Staff Two", UserRole.ServiceStaff);
        db.Users.AddRange(requester, staffOne, staffTwo);

        var now = DateTimeOffset.UtcNow;
        for (var i = 1; i <= 5; i++)
        {
            db.Requests.Add(Request.Submit(
                reference: "REQ-" + i.ToString("D4"),
                title: "Sample request " + i,
                description: "Seeded for local development.",
                categoryId: categories[i % categories.Count].Id,
                requesterId: requester.Id,
                now: now));
        }

        await db.SaveChangesAsync(ct);
    }
}
