using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Access;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.ReferenceData;
using Xunit;

namespace CivicConnect.Application.Tests;

internal sealed class FakeUserRepository : IUserRepository
{
    public List<AppUser> Users { get; } = [];
    public Dictionary<Guid, List<Guid>> Scopes { get; } = [];

    public Task<IReadOnlyList<UserRow>> ListAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<UserRow>>(Users
            .Select(u => new UserRow(u.Id, u.DisplayName, u.Role, u.IsActive, Scopes.GetValueOrDefault(u.Id) ?? []))
            .ToList());

    public Task<AppUser?> FindAsync(Guid userId, CancellationToken ct)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == userId));

    public void Add(AppUser user) => Users.Add(user);

    public Task ReplaceScopesAsync(Guid userId, IReadOnlyCollection<Guid> categoryIds, CancellationToken ct)
    {
        Scopes[userId] = categoryIds.ToList();
        return Task.CompletedTask;
    }
}

/// <summary>FR-019 (AC-019.1) and FR-020 (AC-020.1, AC-020.2): account and role management and its audit entries.</summary>
[Trait("TestCase", "FR-020")]
public class UserServiceTests
{
    private static readonly Guid Admin = Guid.NewGuid();

    private sealed record Setup(
        UserService Service, FakeUserRepository Users, FakeCategoryRepository Categories, FakeAuditRepository Audit,
        AppUser Requester, AppUser Staff);

    private static Setup Build(bool isAdministrator = true)
    {
        var users = new FakeUserRepository();
        var requester = AppUser.Create("Rita Requester", UserRole.Requester);
        var staff = AppUser.Create("Sam Staff", UserRole.ServiceStaff);
        users.Add(requester);
        users.Add(staff);

        var categories = new FakeCategoryRepository();
        categories.Add(Category.Create("Fault"));
        categories.Add(Category.Create("IT"));

        var audit = new FakeAuditRepository();
        var service = new UserService(
            users, categories, audit, new AdminPolicy(isAdministrator), new DirectUnitOfWork(), new FixedClock());
        return new Setup(service, users, categories, audit, requester, staff);
    }

    [Fact]
    [Trait("TestCase", "FR-019")]
    public async Task An_administrator_creates_a_user_and_it_is_audited()
    {
        var s = Build();

        var outcome = await s.Service.CreateAsync(Admin, "  Nia New  ", UserRole.ServiceStaff, CancellationToken.None);

        Assert.Equal(UserOutcome.Done, outcome);
        Assert.Contains(s.Users.Users, u => u.DisplayName == "Nia New" && u.Role == UserRole.ServiceStaff && u.IsActive);
        var entry = Assert.Single(s.Audit.Entries);
        Assert.Equal(("UserCreated", "Nia New", "as ServiceStaff", Admin), (entry.Action, entry.Subject, entry.Detail, entry.ActorUserId));
    }

    [Theory]
    [InlineData("", UserRole.Requester)]
    [InlineData("   ", UserRole.Requester)]
    [InlineData("Valid name", (UserRole)99)]
    public async Task A_missing_name_or_unknown_role_is_refused(string name, UserRole role)
    {
        var s = Build();

        Assert.Equal(UserOutcome.Invalid, await s.Service.CreateAsync(Admin, name, role, CancellationToken.None));
        Assert.Equal(2, s.Users.Users.Count);
        Assert.Empty(s.Audit.Entries);
    }

    [Fact]
    public async Task A_role_change_applies_and_is_audited()
    {
        var s = Build();

        var outcome = await s.Service.ChangeRoleAsync(Admin, s.Requester.Id, UserRole.Management, CancellationToken.None);

        Assert.Equal(UserOutcome.Done, outcome);
        Assert.Equal(UserRole.Management, s.Requester.Role);
        Assert.Equal("Requester to Management", Assert.Single(s.Audit.Entries).Detail);
    }

    [Fact]
    [Trait("TestCase", "FR-019")]
    public async Task Deactivating_and_reactivating_are_audited()
    {
        var s = Build();

        Assert.Equal(UserOutcome.Done, await s.Service.DeactivateAsync(Admin, s.Staff.Id, CancellationToken.None));
        Assert.False(s.Staff.IsActive);
        Assert.Equal(UserOutcome.Done, await s.Service.ReactivateAsync(Admin, s.Staff.Id, CancellationToken.None));
        Assert.True(s.Staff.IsActive);

        Assert.Equal(new[] { "UserDeactivated", "UserReactivated" }, s.Audit.Entries.Select(e => e.Action));
    }

    [Fact]
    public async Task An_administrator_cannot_change_their_own_role_or_deactivate_themselves()
    {
        var s = Build();
        var admin = AppUser.Create("Ada Admin", UserRole.Administrator);
        s.Users.Add(admin);

        Assert.Equal(UserOutcome.OwnAccount, await s.Service.ChangeRoleAsync(admin.Id, admin.Id, UserRole.Requester, CancellationToken.None));
        Assert.Equal(UserOutcome.OwnAccount, await s.Service.DeactivateAsync(admin.Id, admin.Id, CancellationToken.None));
        Assert.Equal(UserRole.Administrator, admin.Role);
        Assert.True(admin.IsActive);
        Assert.Empty(s.Audit.Entries);
    }

    [Fact]
    [Trait("TestCase", "AC-020.2")]
    public async Task Someone_who_is_not_an_administrator_changes_nothing()
    {
        var s = Build(isAdministrator: false);
        var ct = CancellationToken.None;

        Assert.Equal(UserOutcome.Forbidden, await s.Service.CreateAsync(Admin, "Eve", UserRole.Administrator, ct));
        Assert.Equal(UserOutcome.Forbidden, await s.Service.ChangeRoleAsync(Admin, s.Requester.Id, UserRole.Administrator, ct));
        Assert.Equal(UserOutcome.Forbidden, await s.Service.DeactivateAsync(Admin, s.Staff.Id, ct));
        Assert.Equal(UserOutcome.Forbidden, await s.Service.SetScopesAsync(Admin, s.Staff.Id, [], ct));

        Assert.Equal(2, s.Users.Users.Count);
        Assert.Equal(UserRole.Requester, s.Requester.Role);
        Assert.True(s.Staff.IsActive);
        Assert.Empty(s.Audit.Entries);
    }

    [Fact]
    public async Task Staff_scopes_are_replaced_and_audited_with_category_names()
    {
        var s = Build();
        var ids = s.Categories.Items.Select(c => c.Id).ToList();

        var outcome = await s.Service.SetScopesAsync(Admin, s.Staff.Id, ids, CancellationToken.None);

        Assert.Equal(UserOutcome.Done, outcome);
        Assert.Equal(ids, s.Users.Scopes[s.Staff.Id]);
        Assert.Equal("now Fault, IT", Assert.Single(s.Audit.Entries).Detail);
    }

    [Fact]
    public async Task Only_staff_have_scopes_and_only_known_categories_are_accepted()
    {
        var s = Build();
        var ct = CancellationToken.None;

        Assert.Equal(UserOutcome.NotStaff, await s.Service.SetScopesAsync(Admin, s.Requester.Id, [s.Categories.Items[0].Id], ct));
        Assert.Equal(UserOutcome.Invalid, await s.Service.SetScopesAsync(Admin, s.Staff.Id, [Guid.NewGuid()], ct));
        Assert.Empty(s.Users.Scopes);
        Assert.Empty(s.Audit.Entries);
    }

    [Fact]
    public async Task An_unknown_user_is_reported_as_not_found()
    {
        var s = Build();

        Assert.Equal(UserOutcome.NotFound, await s.Service.DeactivateAsync(Admin, Guid.NewGuid(), CancellationToken.None));
    }
}
