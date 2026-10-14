namespace CivicConnect.Domain.Access;

/// <summary>
/// Minimal identity. The authentication mechanism is still open under CR-003,
/// so this carries only what authorisation needs (ADR-005). Accounts are never
/// deleted: a deactivated user keeps their history but can do nothing (AC-019.1).
/// </summary>
public class AppUser
{
    public const int DisplayNameMax = 100;

    private AppUser() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string DisplayName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static AppUser Create(string displayName, UserRole role)
        => string.IsNullOrWhiteSpace(displayName)
            ? throw new ArgumentException("A user needs a display name.", nameof(displayName))
            : new() { DisplayName = displayName.Trim(), Role = role };

    /// <summary>FR-020 (AC-020.1). What the user may do follows the new role at once.</summary>
    public void ChangeRole(UserRole role) => Role = role;

    /// <summary>FR-019 (AC-019.1). The user can no longer sign in or act.</summary>
    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}

public enum UserRole
{
    Requester = 0,
    ServiceStaff = 1,
    Management = 2,
    Administrator = 3,

    /// <summary>ST-5. Sees the high-level summary only (FR-023, CR-007).</summary>
    Sponsor = 4
}
