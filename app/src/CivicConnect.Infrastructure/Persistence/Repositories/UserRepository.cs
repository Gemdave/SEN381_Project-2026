using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(CivicConnectDbContext db) : IUserRepository
{
    public async Task<IReadOnlyList<UserRow>> ListAsync(CancellationToken ct)
    {
        var scopes = await db.StaffCategoryScopes.AsNoTracking()
            .Select(s => new { s.UserId, s.CategoryId })
            .ToListAsync(ct);
        var byUser = scopes.ToLookup(s => s.UserId, s => s.CategoryId);

        var users = await db.Users.AsNoTracking()
            .OrderByDescending(u => u.IsActive).ThenBy(u => u.Role).ThenBy(u => u.DisplayName)
            .ToListAsync(ct);

        return users
            .Select(u => new UserRow(u.Id, u.DisplayName, u.Role, u.IsActive, byUser[u.Id].ToList(), u.Email))
            .ToList();
    }

    public Task<AppUser?> FindAsync(Guid userId, CancellationToken ct)
        => db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    public Task<AppUser?> FindByEmailAsync(string normalizedEmail, CancellationToken ct)
        => db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

    public void Add(AppUser user) => db.Users.Add(user);

    public async Task ReplaceScopesAsync(Guid userId, IReadOnlyCollection<Guid> categoryIds, CancellationToken ct)
    {
        var current = await db.StaffCategoryScopes.Where(s => s.UserId == userId).ToListAsync(ct);
        db.StaffCategoryScopes.RemoveRange(current.Where(s => !categoryIds.Contains(s.CategoryId)));

        var kept = current.Select(s => s.CategoryId).ToHashSet();
        db.StaffCategoryScopes.AddRange(categoryIds
            .Where(id => !kept.Contains(id))
            .Select(id => StaffCategoryScope.Create(userId, id)));
    }
}
