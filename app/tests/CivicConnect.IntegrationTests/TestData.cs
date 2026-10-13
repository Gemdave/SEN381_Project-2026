using CivicConnect.Application.Requests;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Domain.Requests;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Infrastructure.Persistence.Repositories;
using CivicConnect.Infrastructure.Security;

namespace CivicConnect.IntegrationTests;

internal sealed record Seeded(Guid RequestId, Guid RequesterId, Guid StaffId, Guid OtherStaffId, Guid AdminId);

internal static class TestData
{
    /// <summary>One new request with its own requester, two staff members and an administrator.</summary>
    public static async Task<Seeded> CreateAsync(TestDatabaseFixture fixture, string title = "Test request")
    {
        await using var db = fixture.NewContext();

        var category = Category.Create("Cat " + Guid.NewGuid().ToString("N")[..8]);
        var requester = AppUser.Create("Requester", UserRole.Requester);
        var staff = AppUser.Create("Staff", UserRole.ServiceStaff);
        var otherStaff = AppUser.Create("Other staff", UserRole.ServiceStaff);
        var admin = AppUser.Create("Admin", UserRole.Administrator);
        var request = Request.Submit(
            "REQ-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            title,
            "Test description.",
            category.Id,
            requester.Id,
            DateTimeOffset.UtcNow);

        db.AddRange(category, requester, staff, otherStaff, admin, request);

        // FR-008: both staff members cover this category, so ownership rules,
        // not the category scope, decide what each may do.
        db.AddRange(
            StaffCategoryScope.Create(staff.Id, category.Id),
            StaffCategoryScope.Create(otherStaff.Id, category.Id));
        await db.SaveChangesAsync();

        return new Seeded(request.Id, requester.Id, staff.Id, otherStaff.Id, admin.Id);
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
