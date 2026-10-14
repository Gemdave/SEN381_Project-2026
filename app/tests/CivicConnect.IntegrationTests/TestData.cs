using CivicConnect.Application.Requests;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Domain.Requests;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Infrastructure.Persistence.Repositories;
using CivicConnect.Infrastructure.Security;

namespace CivicConnect.IntegrationTests;

internal sealed record Seeded(
    Guid RequestId, Guid RequesterId, Guid StaffId, Guid OtherStaffId, Guid AdminId, Guid ManagerId, Guid SponsorId);

internal static class TestData
{
    /// <summary>One new request with its own requester, two staff members, an administrator, a manager and a sponsor.</summary>
    public static async Task<Seeded> CreateAsync(TestDatabaseFixture fixture, string title = "Test request")
    {
        await using var db = fixture.NewContext();

        var category = Category.Create("Cat " + Guid.NewGuid().ToString("N")[..8]);
        var requester = AppUser.Create("Requester", UserRole.Requester);
        var staff = AppUser.Create("Staff", UserRole.ServiceStaff);
        var otherStaff = AppUser.Create("Other staff", UserRole.ServiceStaff);
        var admin = AppUser.Create("Admin", UserRole.Administrator);
        var manager = AppUser.Create("Manager", UserRole.Management);
        var sponsor = AppUser.Create("Sponsor", UserRole.Sponsor);
        var request = Request.Submit(
            "REQ-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            title,
            "Test description.",
            category.Id,
            requester.Id,
            DateTimeOffset.UtcNow);

        db.AddRange(category, requester, staff, otherStaff, admin, manager, sponsor, request);

        // FR-008: both staff members cover this category, so ownership rules,
        // not the category scope, decide what each may do.
        db.AddRange(
            StaffCategoryScope.Create(staff.Id, category.Id),
            StaffCategoryScope.Create(otherStaff.Id, category.Id));
        await db.SaveChangesAsync();

        return new Seeded(request.Id, requester.Id, staff.Id, otherStaff.Id, admin.Id, manager.Id, sponsor.Id);
    }
}

/// <summary>The real services, wired to one database context.</summary>
internal sealed class Wired(CivicConnectDbContext db)
{
    public AssignmentService Assignment { get; } = new(
        new RequestRepository(db),
        new RequestHistoryRepository(db),
        new AuthorisationPolicy(db),
        new UnitOfWork(db),
        new SystemClock(),
        new FeedbackRepository(db));

    public StatusTransitionService Transitions { get; } = new(
        new RequestRepository(db),
        new RequestHistoryRepository(db),
        new AuthorisationPolicy(db),
        new UnitOfWork(db),
        new SystemClock(),
        new FeedbackRepository(db));
}
