using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Audit;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.ReferenceData;

public enum CategoryOutcome
{
    Done,
    Invalid,
    Duplicate,
    NotFound,
    Forbidden
}

/// <summary>
/// FR-021 (AC-021.1). Administrators add, rename, retire and restore categories
/// with no code change; the submit form reads the live list. Every change is
/// written to the audit trail in the same save (FR-022, NFR-007).
/// </summary>
public sealed class CategoryService(
    ICategoryRepository categories,
    IAuditRepository audit,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const int NameMax = 60;

    public async Task<CategoryOutcome> AddAsync(Guid adminId, string? name, CancellationToken ct)
    {
        var check = await CheckNameAsync(adminId, name, null, ct);
        if (check is not null)
        {
            return check.Value;
        }

        var clean = name!.Trim();
        categories.Add(Category.Create(clean));
        return await SaveAsync(adminId, "CategoryAdded", clean, null, ct);
    }

    public async Task<CategoryOutcome> RenameAsync(Guid adminId, Guid categoryId, string? name, CancellationToken ct)
    {
        var check = await CheckNameAsync(adminId, name, categoryId, ct);
        if (check is not null)
        {
            return check.Value;
        }

        var category = await categories.FindAsync(categoryId, ct);
        if (category is null)
        {
            return CategoryOutcome.NotFound;
        }

        var oldName = category.Name;
        category.Rename(name!);
        return await SaveAsync(adminId, "CategoryRenamed", category.Name, "was " + oldName, ct);
    }

    /// <summary>
    /// FR-016 (CR-005). The target response time for new requests in this
    /// category; requests already submitted keep the due time they were given.
    /// </summary>
    public async Task<CategoryOutcome> SetTargetAsync(Guid adminId, Guid categoryId, int hours, CancellationToken ct)
    {
        if (!await policy.MayAdministerAsync(adminId, ct))
        {
            return CategoryOutcome.Forbidden;
        }

        if (!ResponseTarget.IsValidHours(hours))
        {
            return CategoryOutcome.Invalid;
        }

        var category = await categories.FindAsync(categoryId, ct);
        if (category is null)
        {
            return CategoryOutcome.NotFound;
        }

        var oldHours = category.TargetResponseHours;
        category.SetTarget(hours);
        return await SaveAsync(adminId, "CategoryTargetChanged", category.Name, $"{oldHours} to {hours} hours", ct);
    }

    public Task<CategoryOutcome> RetireAsync(Guid adminId, Guid categoryId, CancellationToken ct)
        => ChangeStateAsync(adminId, categoryId, retire: true, ct);

    public Task<CategoryOutcome> RestoreAsync(Guid adminId, Guid categoryId, CancellationToken ct)
        => ChangeStateAsync(adminId, categoryId, retire: false, ct);

    private async Task<CategoryOutcome> ChangeStateAsync(Guid adminId, Guid categoryId, bool retire, CancellationToken ct)
    {
        if (!await policy.MayAdministerAsync(adminId, ct))
        {
            return CategoryOutcome.Forbidden;
        }

        var category = await categories.FindAsync(categoryId, ct);
        if (category is null)
        {
            return CategoryOutcome.NotFound;
        }

        if (retire)
        {
            category.Retire();
        }
        else
        {
            category.Restore();
        }

        return await SaveAsync(adminId, retire ? "CategoryRetired" : "CategoryRestored", category.Name, null, ct);
    }

    private async Task<CategoryOutcome?> CheckNameAsync(Guid adminId, string? name, Guid? exceptId, CancellationToken ct)
    {
        if (!await policy.MayAdministerAsync(adminId, ct))
        {
            return CategoryOutcome.Forbidden;
        }

        var clean = name?.Trim() ?? string.Empty;
        if (clean.Length == 0 || clean.Length > NameMax)
        {
            return CategoryOutcome.Invalid;
        }

        return await categories.NameTakenAsync(clean, exceptId, ct) ? CategoryOutcome.Duplicate : null;
    }

    private async Task<CategoryOutcome> SaveAsync(Guid adminId, string action, string subject, string? detail, CancellationToken ct)
    {
        audit.Add(AuditEntry.Record(adminId, action, subject, detail, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(ct);
        return CategoryOutcome.Done;
    }
}
