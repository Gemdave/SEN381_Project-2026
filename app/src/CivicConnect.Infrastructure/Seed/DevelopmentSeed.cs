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
    public const string SecondRequester = "Dev Requester Two";
    public const string Sponsor = "Dev Sponsor";
    public const string FormerStaff = "Dev Former Staff";
    public const string OverdueReference = "REQ-0008";

    public static async Task RunAsync(CivicConnectDbContext db, CancellationToken ct = default)
    {
        if (!await db.Categories.AnyAsync(ct))
        {
            await SeedBaseAsync(db, ct);
        }

        await EnsureSecondRequesterAsync(db, ct);
        await EnsureReportingDataAsync(db, ct);
    }

    private static async Task SeedBaseAsync(CivicConnectDbContext db, CancellationToken ct)
    {
        // FR-002. The final list is pending CR-008.
        string[] names = ["Fault", "Equipment", "Security", "IT", "Maintenance", "Lost Property", "Other"];
        var categories = names.Select(Category.Create).ToList();
        db.Categories.AddRange(categories);

        var requester = AppUser.Create("Dev Requester", UserRole.Requester);
        var staffOne = AppUser.Create("Dev Staff One", UserRole.ServiceStaff);
        var staffTwo = AppUser.Create("Dev Staff Two", UserRole.ServiceStaff);
        var manager = AppUser.Create("Dev Manager", UserRole.Management);
        var admin = AppUser.Create("Dev Administrator", UserRole.Administrator);
        db.Users.AddRange(requester, staffOne, staffTwo, manager, admin);

        // FR-008. Split the categories so each staff member sees a different part
        // of the queue, and BB-03 has an out-of-scope request to try.
        string[] staffOneCategories = ["Fault", "Equipment", "Security", "Other"];
        foreach (var category in categories)
        {
            var owner = staffOneCategories.Contains(category.Name) ? staffOne : staffTwo;
            db.StaffCategoryScopes.Add(StaffCategoryScope.Create(owner.Id, category.Id));
        }

        var now = DateTimeOffset.UtcNow;
        for (var i = 1; i <= 5; i++)
        {
            db.Requests.Add(Request.Submit(
                reference: "REQ-" + i.ToString("D4"),
                title: "Sample request " + i,
                description: "Seeded for local development.",
                location: "Main building, room " + (100 + i),
                categoryId: categories[i % categories.Count].Id,
                requesterId: requester.Id,
                now: now.AddMinutes((i - 6) * 10)));
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// BB-05 and NFR-003 need a second requester with requests of their own.
    /// This runs on its own, so a database seeded before it existed gets the
    /// requester too. A database without the development seed (for example the
    /// integration test database) is left alone.
    /// </summary>
    private static async Task EnsureSecondRequesterAsync(CivicConnectDbContext db, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.DisplayName == SecondRequester, ct)
            || !await db.Users.AnyAsync(u => u.DisplayName == "Dev Requester", ct))
        {
            return;
        }

        var fault = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Fault", ct);
        var it = await db.Categories.FirstOrDefaultAsync(c => c.Name == "IT", ct);
        if (fault is null || it is null)
        {
            return;
        }

        var requester = AppUser.Create(SecondRequester, UserRole.Requester);
        db.Users.Add(requester);
        var now = DateTimeOffset.UtcNow;

        db.Requests.Add(Request.Submit(
            reference: "REQ-0006",
            title: "Sample request 6",
            description: "Seeded for local development.",
            location: "Annex, room 6",
            categoryId: fault.Id,
            requesterId: requester.Id,
            now: now));

        db.Requests.Add(Request.Submit(
            reference: "REQ-0007",
            title: "Sample request 7",
            description: "Seeded for local development.",
            location: "Annex, room 7",
            categoryId: it.Id,
            requesterId: requester.Id,
            now: now));

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// FR-016, FR-019 and FR-023 need a sponsor, a deactivated account and a
    /// request already past its target. Like the second requester, this tops up
    /// a database seeded earlier and leaves a database without the seed alone.
    /// </summary>
    private static async Task EnsureReportingDataAsync(CivicConnectDbContext db, CancellationToken ct)
    {
        var requester = await db.Users.FirstOrDefaultAsync(u => u.DisplayName == "Dev Requester", ct);
        var maintenance = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Maintenance", ct);
        if (requester is null || maintenance is null || await db.Users.AnyAsync(u => u.DisplayName == Sponsor, ct))
        {
            return;
        }

        var formerStaff = AppUser.Create(FormerStaff, UserRole.ServiceStaff);
        formerStaff.Deactivate();
        db.Users.AddRange(AppUser.Create(Sponsor, UserRole.Sponsor), formerStaff);

        // Submitted four days ago against the default three-day target, so it is overdue.
        db.Requests.Add(Request.Submit(
            reference: OverdueReference,
            title: "Sample request 8",
            description: "Seeded for local development. Past its target response time.",
            location: "Main building, room 108",
            categoryId: maintenance.Id,
            requesterId: requester.Id,
            now: DateTimeOffset.UtcNow.AddDays(-4),
            targetResponseHours: maintenance.TargetResponseHours));

        await db.SaveChangesAsync(ct);
    }
}
