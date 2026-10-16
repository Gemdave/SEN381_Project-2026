namespace CivicConnect.Domain.Access;

/// <summary>
/// FR-008. Links a staff member to a category they handle. Staff see and work
/// only requests in their scoped categories (AC-008.1, AC-008.2).
/// </summary>
public class StaffCategoryScope
{
    private StaffCategoryScope() { }

    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }

    public static StaffCategoryScope Create(Guid userId, Guid categoryId)
        => userId == Guid.Empty || categoryId == Guid.Empty
            ? throw new ArgumentException("A scope needs a user and a category.")
            : new StaffCategoryScope { UserId = userId, CategoryId = categoryId };
}
