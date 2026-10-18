using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.Audit;

namespace CivicConnect.Application.Access;

public enum UserOutcome
{
    Done,
    Invalid,
    NotFound,
    Forbidden,
    OwnAccount,
    NotStaff
}

/// <summary>
/// FR-019 (AC-019.1) and FR-020 (AC-020.1, AC-020.2). Administrators create and
/// deactivate accounts, change roles and set staff category scopes. Roles are
/// data, so a role change alters what the user may do with no code change; the
/// rules for each role stay in AuthorisationPolicy (ADR-005). An administrator
/// cannot change their own role or deactivate themselves, so the system is never
/// left without an administrator. Every change is audited (FR-022, NFR-007).
/// </summary>
public sealed class UserService(
    IUserRepository users,
    ICategoryRepository categories,
    IAuditRepository audit,
    IAuthorisationPolicy policy,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<UserOutcome> CreateAsync(Guid adminId, string? displayName, UserRole role, CancellationToken ct)
    {
        if (!await policy.MayAdministerAsync(adminId, ct))
        {
            return UserOutcome.Forbidden;
        }

        var name = displayName?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > AppUser.DisplayNameMax || !Enum.IsDefined(role))
        {
            return UserOutcome.Invalid;
        }

        users.Add(AppUser.Create(name, role));
        return await SaveAsync(adminId, "UserCreated", name, "as " + role, ct);
    }
    /*CR-003. Creates an account that can sign in. 
    The administrator sets a temporary password and the user must replace it on first sign-in, 
    so the administrator never knows the password the user keeps.*/
    public async Task<UserOutcome> CreateWithSignInAsync(
        Guid adminId, string? displayName, UserRole role, string? email, string? temporaryPassword, CancellationToken ct)
    {
        if (!await policy.MayAdministerAsync(adminId, ct))
        {
            return UserOutcome.Forbidden;
        }

        var name = displayName?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > AppUser.DisplayNameMax || !Enum.IsDefined(role)
            || !AppUser.IsPlausibleEmail(email))
        {
            return UserOutcome.Invalid;
        }

        var normalized = AppUser.NormalizeEmail(email);
        if (PasswordPolicy.Check(temporaryPassword, normalized) is not null)
        {
            return UserOutcome.WeakPassword;
        }

        if (await users.FindByEmailAsync(normalized, ct) is not null)
        {
            return UserOutcome.EmailTaken;
        }

        var user = AppUser.Create(name, role);
        user.SetCredentials(normalized, hasher.Hash(temporaryPassword!), mustChangePassword: true);
        users.Add(user);
        return await SaveAsync(adminId, "UserCreated", name, "as " + role + ", with sign-in", ct);
    }
    /*CR-003. For a user who has forgotten their password. 
    Sets a new temporary password, clears any lockout and ends their current sessions. 
    Audited, but the password itself never is.*/
        public async Task<UserOutcome> ResetPasswordAsync(Guid adminId, Guid userId, string? temporaryPassword, CancellationToken ct)
    {
        var (outcome, user) = await TargetAsync(adminId, userId, ct);
        if (user is null)
        {
            return outcome;
        }

        if (user.Email is null)
        {
            return UserOutcome.Invalid;
        }

        if (PasswordPolicy.Check(temporaryPassword, user.Email) is not null)
        {
            return UserOutcome.WeakPassword;
        }

        user.SetCredentials(user.Email, hasher.Hash(temporaryPassword!), mustChangePassword: true);
        return await SaveAsync(adminId, "UserPasswordReset", user.DisplayName, null, ct);
    }

    public async Task<UserOutcome> ChangeRoleAsync(Guid adminId, Guid userId, UserRole role, CancellationToken ct)
    {
        if (!Enum.IsDefined(role))
        {
            return UserOutcome.Invalid;
        }

        var (outcome, user) = await TargetAsync(adminId, userId, ct);
        if (user is null)
        {
            return outcome;
        }

        var oldRole = user.Role;
        user.ChangeRole(role);
        return await SaveAsync(adminId, "UserRoleChanged", user.DisplayName, $"{oldRole} to {role}", ct);
    }

    public async Task<UserOutcome> DeactivateAsync(Guid adminId, Guid userId, CancellationToken ct)
    {
        var (outcome, user) = await TargetAsync(adminId, userId, ct);
        if (user is null)
        {
            return outcome;
        }

        user.Deactivate();
        return await SaveAsync(adminId, "UserDeactivated", user.DisplayName, null, ct);
    }

    public async Task<UserOutcome> ReactivateAsync(Guid adminId, Guid userId, CancellationToken ct)
    {
        var (outcome, user) = await TargetAsync(adminId, userId, ct);
        if (user is null)
        {
            return outcome;
        }

        user.Reactivate();
        return await SaveAsync(adminId, "UserReactivated", user.DisplayName, null, ct);
    }

    /// <summary>FR-008 with FR-020. Only service staff have category scopes.</summary>
    public async Task<UserOutcome> SetScopesAsync(
        Guid adminId, Guid userId, IReadOnlyCollection<Guid> categoryIds, CancellationToken ct)
    {
        if (!await policy.MayAdministerAsync(adminId, ct))
        {
            return UserOutcome.Forbidden;
        }

        var user = await users.FindAsync(userId, ct);
        if (user is null)
        {
            return UserOutcome.NotFound;
        }

        if (user.Role != UserRole.ServiceStaff)
        {
            return UserOutcome.NotStaff;
        }

        var known = (await categories.ListAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name);
        var chosen = categoryIds.Distinct().ToList();
        if (chosen.Any(id => !known.ContainsKey(id)))
        {
            return UserOutcome.Invalid;
        }

        await users.ReplaceScopesAsync(userId, chosen, ct);
        var names = chosen.Count == 0 ? "none" : string.Join(", ", chosen.Select(id => known[id]).Order());
        return await SaveAsync(adminId, "UserScopesChanged", user.DisplayName, "now " + names, ct);
    }

    private async Task<(UserOutcome Outcome, AppUser? User)> TargetAsync(Guid adminId, Guid userId, CancellationToken ct)
    {
        if (!await policy.MayAdministerAsync(adminId, ct))
        {
            return (UserOutcome.Forbidden, null);
        }

        if (userId == adminId)
        {
            return (UserOutcome.OwnAccount, null);
        }

        var user = await users.FindAsync(userId, ct);
        return user is null ? (UserOutcome.NotFound, null) : (UserOutcome.Done, user);
    }

    private async Task<UserOutcome> SaveAsync(Guid adminId, string action, string subject, string? detail, CancellationToken ct)
    {
        audit.Add(AuditEntry.Record(adminId, action, subject, detail, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(ct);
        return UserOutcome.Done;
    }
}
