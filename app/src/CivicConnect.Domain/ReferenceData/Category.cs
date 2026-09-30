namespace CivicConnect.Domain.ReferenceData;

/// <summary>
/// FR-002 and FR-021. A controlled list held as reference data, so an
/// administrator can retire a category without a code change. Retired rows stay
/// so historical requests still resolve their category.
/// </summary>
public class Category
{
    private Category() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public bool IsRetired { get; private set; }

    public static Category Create(string name)
        => string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("A category needs a name.", nameof(name))
            : new Category { Name = name };

    public void Retire() => IsRetired = true;
}
