using CivicConnect.Application.Abstractions;
using CivicConnect.Application.ReferenceData;
using CivicConnect.Domain.Audit;
using CivicConnect.Domain.ReferenceData;
using Xunit;

namespace CivicConnect.Application.Tests;

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    public List<Category> Items { get; } = [];

    public Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Category>>(Items);

    public Task<Category?> FindAsync(Guid categoryId, CancellationToken ct)
        => Task.FromResult(Items.FirstOrDefault(c => c.Id == categoryId));

    public Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken ct)
        => Task.FromResult(Items.Any(c => c.Id != exceptId && string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));

    public void Add(Category category) => Items.Add(category);
}

internal sealed class FakeAuditRepository : IAuditRepository
{
    public List<AuditEntry> Entries { get; } = [];

    public void Add(AuditEntry entry) => Entries.Add(entry);

    public Task<IReadOnlyList<AuditItem>> RecentAsync(AuditKind kind, int take, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<AuditItem>>([]);
}

internal sealed class AdminPolicy(bool isAdministrator) : IAuthorisationPolicy
{
    public Task<bool> MayAsync(Guid userId, RequestAction action, Guid requestId, CancellationToken ct)
        => Task.FromResult(false);

    public Task<bool> MayAdministerAsync(Guid userId, CancellationToken ct) => Task.FromResult(isAdministrator);
}

/// <summary>FR-021 (AC-021.1) and FR-022: category maintenance and its audit entries.</summary>
[Trait("TestCase", "FR-021")]
public class CategoryServiceTests
{
    private static readonly Guid Admin = Guid.NewGuid();

    private static (CategoryService Service, FakeCategoryRepository Categories, FakeAuditRepository Audit) Build(
        bool isAdministrator = true)
    {
        var categories = new FakeCategoryRepository();
        categories.Add(Category.Create("Fault"));
        var audit = new FakeAuditRepository();
        var service = new CategoryService(
            categories, audit, new AdminPolicy(isAdministrator), new DirectUnitOfWork(), new FixedClock());
        return (service, categories, audit);
    }

    [Fact]
    public async Task An_administrator_adds_a_category_and_it_is_audited()
    {
        var (service, categories, audit) = Build();

        var outcome = await service.AddAsync(Admin, "  Parks  ", CancellationToken.None);

        Assert.Equal(CategoryOutcome.Done, outcome);
        Assert.Contains(categories.Items, c => c.Name == "Parks" && !c.IsRetired);
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(("CategoryAdded", "Parks", Admin), (entry.Action, entry.Subject, entry.ActorUserId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task A_missing_name_is_refused_and_nothing_changes(string? name)
    {
        var (service, categories, audit) = Build();

        Assert.Equal(CategoryOutcome.Invalid, await service.AddAsync(Admin, name, CancellationToken.None));
        Assert.Single(categories.Items);
        Assert.Empty(audit.Entries);
    }

    [Fact]
    public async Task A_name_over_the_limit_is_refused()
    {
        var (service, _, _) = Build();

        var outcome = await service.AddAsync(Admin, new string('a', CategoryService.NameMax + 1), CancellationToken.None);

        Assert.Equal(CategoryOutcome.Invalid, outcome);
    }

    [Fact]
    public async Task A_duplicate_name_is_refused_regardless_of_case()
    {
        var (service, categories, audit) = Build();

        Assert.Equal(CategoryOutcome.Duplicate, await service.AddAsync(Admin, "fault", CancellationToken.None));
        Assert.Single(categories.Items);
        Assert.Empty(audit.Entries);
    }

    [Fact]
    public async Task Retire_restore_and_rename_each_change_the_category_and_are_audited()
    {
        var (service, categories, audit) = Build();
        var fault = categories.Items[0];

        Assert.Equal(CategoryOutcome.Done, await service.RetireAsync(Admin, fault.Id, CancellationToken.None));
        Assert.True(fault.IsRetired);
        Assert.Equal(CategoryOutcome.Done, await service.RestoreAsync(Admin, fault.Id, CancellationToken.None));
        Assert.False(fault.IsRetired);
        Assert.Equal(CategoryOutcome.Done, await service.RenameAsync(Admin, fault.Id, "Faults", CancellationToken.None));
        Assert.Equal("Faults", fault.Name);

        Assert.Equal(new[] { "CategoryRetired", "CategoryRestored", "CategoryRenamed" }, audit.Entries.Select(e => e.Action));
        Assert.Equal("was Fault", audit.Entries[^1].Detail);
    }

    [Fact]
    public async Task Someone_who_is_not_an_administrator_changes_nothing()
    {
        var (service, categories, audit) = Build(isAdministrator: false);
        var fault = categories.Items[0];

        Assert.Equal(CategoryOutcome.Forbidden, await service.AddAsync(Admin, "Parks", CancellationToken.None));
        Assert.Equal(CategoryOutcome.Forbidden, await service.RetireAsync(Admin, fault.Id, CancellationToken.None));
        Assert.Single(categories.Items);
        Assert.False(fault.IsRetired);
        Assert.Empty(audit.Entries);
    }

    [Fact]
    public async Task An_unknown_category_is_reported_as_not_found()
    {
        var (service, _, _) = Build();

        Assert.Equal(CategoryOutcome.NotFound, await service.RetireAsync(Admin, Guid.NewGuid(), CancellationToken.None));
    }
}
