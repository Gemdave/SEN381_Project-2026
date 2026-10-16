using CivicConnect.Domain.Requests;

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

    /// <summary>FR-016 (CR-005). How long staff have to respond to a request in this category.</summary>
    public int TargetResponseHours { get; private set; } = ResponseTarget.DefaultHours;

    public static Category Create(string name)
        => string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("A category needs a name.", nameof(name))
            : new Category { Name = name };

    /// <summary>FR-016 and FR-021. Applies to requests submitted from now on.</summary>
    public void SetTarget(int hours)
        => TargetResponseHours = ResponseTarget.IsValidHours(hours)
            ? hours
            : throw new ArgumentOutOfRangeException(nameof(hours));

    public void Retire() => IsRetired = true;

    /// <summary>FR-021. Retired categories can come back without losing their requests.</summary>
    public void Restore() => IsRetired = false;

    /// <summary>FR-021 (AC-021.1). Requests keep their category id, so they follow the new name.</summary>
    public void Rename(string name)
        => Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("A category needs a name.", nameof(name))
            : name.Trim();
}
