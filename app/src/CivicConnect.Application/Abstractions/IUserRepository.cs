using CivicConnect.Domain.Access;

namespace CivicConnect.Application.Abstractions;

/// <summary>One account as the administrator's Users page lists it.</summary>
public sealed record UserRow(
    Guid Id, string DisplayName, UserRole Role, bool IsActive, IReadOnlyList<Guid> ScopedCategoryIds);

/// <summary>
/// FR-019 and FR-020. Accounts are never deleted, so history keeps its actors;
/// deactivation is the only way to remove access (AC-019.1).
/// </summary>
public interface IUserRepository
{
    Task<IReadOnlyList<UserRow>> ListAsync(CancellationToken ct);

    Task<AppUser?> FindAsync(Guid userId, CancellationToken ct);

    void Add(AppUser user);

    /// <summary>FR-008. Replaces the staff member's category scopes with exactly these.</summary>
    Task ReplaceScopesAsync(Guid userId, IReadOnlyCollection<Guid> categoryIds, CancellationToken ct);
}
