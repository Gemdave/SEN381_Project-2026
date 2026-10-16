using CivicConnect.Domain.ReferenceData;

namespace CivicConnect.Application.Abstractions;

/// <summary>FR-021. The controlled category list, including retired categories.</summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAllAsync(CancellationToken ct);

    Task<Category?> FindAsync(Guid categoryId, CancellationToken ct);

    /// <summary>Names are unique regardless of case; the category being renamed is ignored.</summary>
    Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken ct);

    void Add(Category category);
}
