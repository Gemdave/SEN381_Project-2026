using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;

namespace CivicConnect.Application.Access;

public enum SignInOutcome
{
    Succeeded,
    InvalidCredentials,
    LockedOut
}

/// <summary>What the web layer needs to start a session. Nothing here is secret.</summary>
public sealed record SignInDecision(
    SignInOutcome Outcome,
    Guid UserId = default,
    string DisplayName = "",
    bool MustChangePassword = false,
    Guid SecurityStamp = default);

public enum ChangePasswordOutcome
{
    Done,
    WrongCurrentPassword,
    WeakPassword,
    SameAsCurrent,
    NotFound
}

public sealed record ChangePasswordResult(ChangePasswordOutcome Outcome, Guid SecurityStamp = default);

/*CR-003 (closes RSK-012 once deployed). Checks an email and password and keeps the failed-attempt count that locks an account after repeated wrong passwords. 
The failure message is the same for an unknown email, a wrong password and a deactivated account, 
so the form does not reveal which emails exist (AC-019.1: a deactivated user cannot sign in).*/
public sealed class SignInService(
    IUserRepository users,
    IPasswordHasher hasher,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<SignInDecision> SignInAsync(string? email, string? password, CancellationToken ct)
    {
        // An absurdly long value is never a real password, and hashing it is wasted work.
        var attempt = password is { Length: <= PasswordPolicy.MaxLength } ? password : string.Empty;

        var normalized = AppUser.NormalizeEmail(email);
        var user = normalized.Length == 0 ? null : await users.FindByEmailAsync(normalized, ct);
        var now = clock.UtcNow;

        if (user is null || !user.HasSignIn)
        {
            hasher.Verify(attempt, null);
            return new SignInDecision(SignInOutcome.InvalidCredentials);
        }

        if (user.IsLockedOut(now))
        {
            return new SignInDecision(SignInOutcome.LockedOut);
        }

        var correct = hasher.Verify(attempt, user.PasswordHash);

        if (!correct || !user.IsActive)
        {
            if (!correct)
            {
                user.RegisterFailedSignIn(now, MaxFailedAttempts, LockoutDuration);
                await unitOfWork.SaveChangesAsync(ct);
            }

            return new SignInDecision(user.IsLockedOut(now) ? SignInOutcome.LockedOut : SignInOutcome.InvalidCredentials);
        }

        user.RegisterSuccessfulSignIn();
        await unitOfWork.SaveChangesAsync(ct);
        return new SignInDecision(SignInOutcome.Succeeded, user.Id, user.DisplayName, user.MustChangePassword, user.SecurityStamp);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId, string? currentPassword, string? newPassword, CancellationToken ct)
    {
        var user = await users.FindAsync(userId, ct);
        if (user is not { IsActive: true } || !user.HasSignIn)
        {
            return new ChangePasswordResult(ChangePasswordOutcome.NotFound);
        }

        if (!hasher.Verify(currentPassword is { Length: <= PasswordPolicy.MaxLength } ? currentPassword : string.Empty, user.PasswordHash))
        {
            return new ChangePasswordResult(ChangePasswordOutcome.WrongCurrentPassword);
        }

        if (PasswordPolicy.Check(newPassword, user.Email) is not null)
        {
            return new ChangePasswordResult(ChangePasswordOutcome.WeakPassword);
        }

        if (newPassword == currentPassword)
        {
            return new ChangePasswordResult(ChangePasswordOutcome.SameAsCurrent);
        }

        user.ChangePassword(hasher.Hash(newPassword!));
        await unitOfWork.SaveChangesAsync(ct);
        return new ChangePasswordResult(ChangePasswordOutcome.Done, user.SecurityStamp);
    }
}
