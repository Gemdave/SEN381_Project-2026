namespace CivicConnect.Domain.Access;

/// <summary>
/// Minimal identity for M2. The authentication mechanism is still open under
/// CR-003, so this carries only what authorisation needs (ADR-005).
/// </summary>
public class AppUser
{
    private AppUser() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string DisplayName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    public static AppUser Create(string displayName, UserRole role)
        => new() { DisplayName = displayName, Role = role };
}

public enum UserRole
{
    Requester = 0,
    ServiceStaff = 1,
    Management = 2,
    Administrator = 3
}
