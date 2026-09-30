using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;

namespace CivicConnect.Core.Services;

// FR-002. The form always reads the current list, so no code change is needed to add one.
public class CategoryService(IUnitOfWorkFactory factory)
{
    public async Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken ct = default)
    {
        await using var uow = await factory.BeginAsync(ct);
        return await uow.Categories.ListActiveAsync(ct);
    }
}
