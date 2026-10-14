using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(CivicConnectDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken ct)
        => await db.Categories.AsNoTracking().OrderBy(c => c.IsRetired).ThenBy(c => c.Name).ToListAsync(ct);

    public Task<Category?> FindAsync(Guid categoryId, CancellationToken ct)
        => db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);

    public Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken ct)
    {
        var lower = name.Trim().ToLower();
        return db.Categories.AnyAsync(c => c.Name.ToLower() == lower && c.Id != exceptId, ct);
    }

    public void Add(Category category) => db.Categories.Add(category);
}
