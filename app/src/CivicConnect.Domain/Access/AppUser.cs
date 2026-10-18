namespace CivicConnect.Domain.Access;

/*Identity and sign-in details. Authorization reads only the role and the active flag (ADR-005); 
the email, password hash, lockout counters and security stamp exist for CR-003 (real sign-in). 
Accounts are never deleted: a deactivated user keeps their history but can do nothing and cannot sign in (AC-019.1).
The password itself is never stored, only a salted hash.*/
public class AppUser
{
    public const int DisplayNameMax = 100;
    public const int EmailMax = 254;
    public const int PasswordHashMax = 255;

    private AppUser() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string DisplayName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    /*-----Lower case and trimmed. Null for an account with no sign-in set up (for example the development seed).-----*/
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    /// <summary>True after an administrator sets or resets the password: the user must choose their own before doing anything else.</summary>
    public bool MustChangePassword { get; private set; }
    public int FailedSignIns { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    /// <summary>Changes whenever the credentials change, which ends every session signed in before that.</summary>
    public Guid SecurityStamp { get; private set; } = Guid.NewGuid();
    public bool HasSignIn => Email is not null && PasswordHash is not null;
    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    /*-----A deliberately simple check: one @, something either side, a dot in the domain, no spaces.-----*/
    public static bool IsPlausibleEmail(string? email)
    {
        var value = NormalizeEmail(email);
        var at = value.IndexOf('@');
        return value.Length is > 3 and <= EmailMax
            && at > 0
            && at == value.LastIndexOf('@')
            && at < value.Length - 3
            && value.IndexOf('.', at) > at + 1
            && !value.EndsWith('.')
            && !value.Any(char.IsWhiteSpace);
    }
    public static AppUser Create(string displayName, UserRole role)
        => string.IsNullOrWhiteSpace(displayName)
            ? throw new ArgumentException("A user needs a display name.", nameof(displayName))
            : new() { DisplayName = displayName.Trim(), Role = role };

    /// <summary>FR-020 (AC-020.1). What the user may do follows the new role at once.</summary>
    public void ChangeRole(UserRole role) => Role = role;

    /// <summary>FR-019 (AC-019.1). The user can no longer sign in or act.</summary>
    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
    /*FR-019. Sets or replaces the sign-in details, clears any lockout and ends existing sessions. 
    The caller supplies a hash, never the password.*/
    public void SetCredentials(string email, string passwordHash, bool mustChangePassword)
    {
        if (!IsPlausibleEmail(email))
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        MustChangePassword = mustChangePassword;
        FailedSignIns = 0;
        LockedUntil = null;
        SecurityStamp = Guid.NewGuid();
    }
    /*-----The user chose a new password. Other sessions end; the caller signs this one in again.-----*/
    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
        MustChangePassword = false;
        SecurityStamp = Guid.NewGuid();
    }
    public bool IsLockedOut(DateTimeOffset now) => LockedUntil is { } until && until > now;

    /*-----Counts a wrong password. After maxAttempts in a row the account locks for lockFor. Attempts while locked do not extend the lock.-----*/
    public void RegisterFailedSignIn(DateTimeOffset now, int maxAttempts, TimeSpan lockFor)
    {
        if (IsLockedOut(now))
        {
            return;
        }

        FailedSignIns++;
        if (FailedSignIns >= maxAttempts)
        {
            LockedUntil = now + lockFor;
            FailedSignIns = 0;
        }
    }
    public void RegisterSuccessfulSignIn()
    {
        FailedSignIns = 0;
        LockedUntil = null;
    }
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
