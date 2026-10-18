using CivicConnect.Application.Access;
using CivicConnect.Domain.Access;
using Xunit;

namespace CivicConnect.Application.Tests;

/// <summary>CR-003 (RSK-012): password rules, sign-in, lockout, change and reset.</summary>
[Trait("TestCase", "CR-003")]
public class SignInTests
{
    private const string Email = "nia@example.org";
    private const string Password = "correct horse battery";
    private static readonly Guid Admin = Guid.NewGuid();
    private static readonly DateTimeOffset Start = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    private sealed record Setup(
        SignInService Authentication, UserService Users, FakeUserRepository Repository,
        FakeAuditRepository Audit, MutableClock Clock, AppUser User);

    private static Setup Build(bool active = true, bool mustChange = false)
    {
        var repository = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var clock = new MutableClock(Start);
        var user = AppUser.Create("Nia Staff", UserRole.ServiceStaff);
        user.SetCredentials(Email, hasher.Hash(Password), mustChange);
        if (!active)
        {
            user.Deactivate();
        }

        repository.Add(user);

        var audit = new FakeAuditRepository();
        return new Setup(
            new SignInService(repository, hasher, new DirectUnitOfWork(), clock),
            new UserService(repository, new FakeCategoryRepository(), audit, new AdminPolicy(true), new DirectUnitOfWork(), clock, hasher),
            repository, audit, clock, user);
    }

    // ---- password rules

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short1")]
    [InlineData("elevenchars")]
    [InlineData("password1234")]
    [InlineData("aaaaaaaaaaaaaaaa")]
    public void Weak_passwords_are_refused(string? password)
        => Assert.NotNull(PasswordPolicy.Check(password, Email));

    [Fact]
    public void A_long_passphrase_is_accepted_without_symbols()
        => Assert.Null(PasswordPolicy.Check("three plain words here", Email));

    [Fact]
    public void The_email_address_is_not_an_acceptable_password()
        => Assert.NotNull(PasswordPolicy.Check("nia@example.org", Email));

    [Fact]
    public void A_password_over_the_maximum_length_is_refused()
        => Assert.NotNull(PasswordPolicy.Check(new string('x', PasswordPolicy.MaxLength + 1) + "y", Email));

    // ---- sign in

    [Fact]
    public async Task The_right_email_and_password_sign_in()
    {
        var s = Build();

        var result = await s.Authentication.SignInAsync("  NIA@Example.org ", Password, CancellationToken.None);

        Assert.Equal(SignInOutcome.Succeeded, result.Outcome);
        Assert.Equal(s.User.Id, result.UserId);
        Assert.Equal(s.User.SecurityStamp, result.SecurityStamp);
        Assert.False(result.MustChangePassword);
    }

    [Theory]
    [InlineData("nia@example.org", "wrong password here")]
    [InlineData("nobody@example.org", Password)]
    [InlineData("", Password)]
    [InlineData(null, null)]
    public async Task A_wrong_password_or_unknown_email_gives_the_same_refusal(string? email, string? password)
    {
        var s = Build();

        var result = await s.Authentication.SignInAsync(email, password, CancellationToken.None);

        Assert.Equal(SignInOutcome.InvalidCredentials, result.Outcome);
        Assert.Equal(Guid.Empty, result.UserId);
    }

    /// <summary>AC-019.1: a deactivated user cannot sign in, and is told no more than anyone else.</summary>
    [Fact]
    public async Task A_deactivated_user_cannot_sign_in()
    {
        var s = Build(active: false);

        var result = await s.Authentication.SignInAsync(Email, Password, CancellationToken.None);

        Assert.Equal(SignInOutcome.InvalidCredentials, result.Outcome);
    }

    [Fact]
    public async Task An_account_with_no_sign_in_set_up_cannot_sign_in()
    {
        var s = Build();
        var seeded = AppUser.Create("Dev Seed", UserRole.Requester);
        s.Repository.Add(seeded);

        Assert.Equal(SignInOutcome.InvalidCredentials, (await s.Authentication.SignInAsync("dev@example.org", Password, CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task A_user_who_must_change_their_password_is_told_so()
    {
        var s = Build(mustChange: true);

        var result = await s.Authentication.SignInAsync(Email, Password, CancellationToken.None);

        Assert.Equal(SignInOutcome.Succeeded, result.Outcome);
        Assert.True(result.MustChangePassword);
    }

    // ---- lockout

    [Fact]
    public async Task Repeated_wrong_passwords_lock_the_account_and_even_the_right_password_is_then_refused()
    {
        var s = Build();

        for (var i = 1; i < SignInService.MaxFailedAttempts; i++)
        {
            Assert.Equal(SignInOutcome.InvalidCredentials, (await s.Authentication.SignInAsync(Email, "wrong password " + i, CancellationToken.None)).Outcome);
        }

        Assert.Equal(SignInOutcome.LockedOut, (await s.Authentication.SignInAsync(Email, "wrong password last", CancellationToken.None)).Outcome);
        Assert.Equal(SignInOutcome.LockedOut, (await s.Authentication.SignInAsync(Email, Password, CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task The_lock_ends_after_the_lockout_period()
    {
        var s = Build();
        for (var i = 0; i < SignInService.MaxFailedAttempts; i++)
        {
            await s.Authentication.SignInAsync(Email, "wrong password " + i, CancellationToken.None);
        }

        s.Clock.UtcNow = Start + SignInService.LockoutDuration + TimeSpan.FromSeconds(1);

        Assert.Equal(SignInOutcome.Succeeded, (await s.Authentication.SignInAsync(Email, Password, CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task A_successful_sign_in_clears_the_failed_attempts()
    {
        var s = Build();
        for (var i = 0; i < SignInService.MaxFailedAttempts - 1; i++)
        {
            await s.Authentication.SignInAsync(Email, "wrong password " + i, CancellationToken.None);
        }

        await s.Authentication.SignInAsync(Email, Password, CancellationToken.None);

        Assert.Equal(0, s.User.FailedSignIns);
        Assert.Equal(SignInOutcome.InvalidCredentials, (await s.Authentication.SignInAsync(Email, "wrong password again", CancellationToken.None)).Outcome);
    }

    // ---- change password

    [Fact]
    public async Task Changing_the_password_needs_the_current_one_and_ends_other_sessions()
    {
        var s = Build(mustChange: true);
        var oldStamp = s.User.SecurityStamp;

        var wrong = await s.Authentication.ChangePasswordAsync(s.User.Id, "not the current one", "a brand new passphrase", CancellationToken.None);
        Assert.Equal(ChangePasswordOutcome.WrongCurrentPassword, wrong.Outcome);

        var done = await s.Authentication.ChangePasswordAsync(s.User.Id, Password, "a brand new passphrase", CancellationToken.None);

        Assert.Equal(ChangePasswordOutcome.Done, done.Outcome);
        Assert.NotEqual(oldStamp, done.SecurityStamp);
        Assert.False(s.User.MustChangePassword);
        Assert.Equal(SignInOutcome.Succeeded, (await s.Authentication.SignInAsync(Email, "a brand new passphrase", CancellationToken.None)).Outcome);
        Assert.Equal(SignInOutcome.InvalidCredentials, (await s.Authentication.SignInAsync(Email, Password, CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task A_weak_or_unchanged_new_password_is_refused()
    {
        var s = Build();

        Assert.Equal(ChangePasswordOutcome.WeakPassword,
            (await s.Authentication.ChangePasswordAsync(s.User.Id, Password, "short", CancellationToken.None)).Outcome);
        Assert.Equal(ChangePasswordOutcome.SameAsCurrent,
            (await s.Authentication.ChangePasswordAsync(s.User.Id, Password, Password, CancellationToken.None)).Outcome);
    }

    // ---- administrator creates and resets

    [Fact]
    public async Task An_administrator_creates_a_user_who_can_sign_in_and_must_change_the_password()
    {
        var s = Build();

        var outcome = await s.Users.CreateWithSignInAsync(
            Admin, " Rita Requester ", UserRole.Requester, " Rita@Example.org ", "temporary passphrase", CancellationToken.None);

        Assert.Equal(UserOutcome.Done, outcome);
        var created = s.Repository.Users.Single(u => u.DisplayName == "Rita Requester");
        Assert.Equal("rita@example.org", created.Email);
        Assert.True(created.MustChangePassword);
        Assert.Contains(s.Audit.Entries, e => e.Action == "UserCreated" && e.Subject == "Rita Requester");
        Assert.DoesNotContain(s.Audit.Entries, e => (e.Detail ?? string.Empty).Contains("temporary passphrase"));

        var signIn = await s.Authentication.SignInAsync("rita@example.org", "temporary passphrase", CancellationToken.None);
        Assert.Equal(SignInOutcome.Succeeded, signIn.Outcome);
        Assert.True(signIn.MustChangePassword);
    }

    [Theory]
    [InlineData("not an email", "temporary passphrase", UserOutcome.Invalid)]
    [InlineData("rita@example.org", "short", UserOutcome.WeakPassword)]
    [InlineData(Email, "temporary passphrase", UserOutcome.EmailTaken)]
    public async Task Bad_sign_in_details_are_refused_and_nothing_is_created(string email, string password, UserOutcome expected)
    {
        var s = Build();
        var before = s.Repository.Users.Count;

        Assert.Equal(expected, await s.Users.CreateWithSignInAsync(Admin, "Rita", UserRole.Requester, email, password, CancellationToken.None));
        Assert.Equal(before, s.Repository.Users.Count);
    }

    [Fact]
    public async Task Resetting_a_password_unlocks_the_account_and_ends_its_sessions()
    {
        var s = Build();
        for (var i = 0; i < SignInService.MaxFailedAttempts; i++)
        {
            await s.Authentication.SignInAsync(Email, "wrong password " + i, CancellationToken.None);
        }

        var oldStamp = s.User.SecurityStamp;
        var outcome = await s.Users.ResetPasswordAsync(Admin, s.User.Id, "another temporary phrase", CancellationToken.None);

        Assert.Equal(UserOutcome.Done, outcome);
        Assert.NotEqual(oldStamp, s.User.SecurityStamp);
        Assert.True(s.User.MustChangePassword);
        Assert.Contains(s.Audit.Entries, e => e.Action == "UserPasswordReset");
        Assert.Equal(SignInOutcome.Succeeded, (await s.Authentication.SignInAsync(Email, "another temporary phrase", CancellationToken.None)).Outcome);
    }

    [Fact]
    public async Task Only_an_administrator_creates_or_resets_sign_in_details()
    {
        var repository = new FakeUserRepository();
        var target = AppUser.Create("Sam", UserRole.Requester);
        target.SetCredentials(Email, "fake$whatever", false);
        repository.Add(target);
        var service = new UserService(
            repository, new FakeCategoryRepository(), new FakeAuditRepository(), new AdminPolicy(false),
            new DirectUnitOfWork(), new FixedClock(), new FakePasswordHasher());

        Assert.Equal(UserOutcome.Forbidden,
            await service.CreateWithSignInAsync(Guid.NewGuid(), "Eve", UserRole.Administrator, "eve@example.org", "temporary passphrase", CancellationToken.None));
        Assert.Equal(UserOutcome.Forbidden,
            await service.ResetPasswordAsync(Guid.NewGuid(), target.Id, "temporary passphrase", CancellationToken.None));
    }
}
