using CivicConnect.Domain.Access;

namespace CivicConnect.Application.Abstractions;
// One account as the administrator's Users page lists it.
public sealed record UserRow(
    Guid Id, string DisplayName, UserRole Role, bool IsActive, IReadOnlyList<Guid> ScopedCategoryIds);
/*FR-019 and FR-020. Accounts are never deleted, so history keeps its actors;
 deactivation is the only way to remove access (AC-019.1).*/
public interface IUserRepository
{
    Task<IReadOnlyList<UserRow>> ListAsync(CancellationToken ct);
    Task<AppUser?> FindAsync(Guid userId, CancellationToken ct);
    //CR-003. The argument is already normalized (AppUser.NormalizeEmail).
    Task<AppUser?> FindByEmailAsync(string normalizedEmail, CancellationToken ct);
    
    void Add(AppUser user);
    // FR-008. Replaces the staff member's category scopes with exactly these.
    Task ReplaceScopesAsync(Guid userId, IReadOnlyCollection<Guid> categoryIds, CancellationToken ct);
}
