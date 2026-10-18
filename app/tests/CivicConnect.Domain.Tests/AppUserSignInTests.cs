using CivicConnect.Domain.Access;
using Xunit;

namespace CivicConnect.Domain.Tests;

/// <summary>CR-003: the sign-in details on a user and the lockout rule.</summary>
[Trait("TestCase", "CR-003")]
public class AppUserSignInTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lock = TimeSpan.FromMinutes(15);

    private static AppUser WithSignIn()
    {
        var user = AppUser.Create("Nia", UserRole.ServiceStaff);
        user.SetCredentials("Nia@Example.org ", "hash", mustChangePassword: false);
        return user;
    }

    [Theory]
    [InlineData("a@b.co", true)]
    [InlineData("  Nia@Example.org ", true)]
    [InlineData("first.last+tag@sub.example.org", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("no-at-sign.example.org", false)]
    [InlineData("two@@example.org", false)]
    [InlineData("a@b", false)]
    [InlineData("a@.com", false)]
    [InlineData("a b@example.org", false)]
    [InlineData("@example.org", false)]
    public void Email_addresses_are_checked_simply(string? email, bool expected)
        => Assert.Equal(expected, AppUser.IsPlausibleEmail(email));

    [Fact]
    public void Credentials_are_stored_normalised_and_a_new_user_has_none()
    {
        var plain = AppUser.Create("Seed", UserRole.Requester);
        Assert.False(plain.HasSignIn);

        var user = WithSignIn();
        Assert.True(user.HasSignIn);
        Assert.Equal("nia@example.org", user.Email);
    }

    [Fact]
    public void Credentials_need_a_valid_email_and_a_hash()
    {
        var user = AppUser.Create("Nia", UserRole.Requester);
        Assert.Throws<ArgumentException>(() => user.SetCredentials("not an email", "hash", true));
        Assert.Throws<ArgumentException>(() => user.SetCredentials("nia@example.org", " ", true));
    }

    [Fact]
    public void Setting_credentials_or_changing_the_password_replaces_the_security_stamp()
    {
        var user = WithSignIn();
        var first = user.SecurityStamp;

        user.ChangePassword("new hash");
        var second = user.SecurityStamp;
        user.SetCredentials("nia@example.org", "reset hash", true);

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, user.SecurityStamp);
        Assert.True(user.MustChangePassword);
    }

    [Fact]
    public void Five_failures_lock_the_account_for_the_lockout_period_and_a_lock_is_not_extended()
    {
        var user = WithSignIn();

        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedSignIn(Now, 5, Lock);
            Assert.False(user.IsLockedOut(Now));
        }

        user.RegisterFailedSignIn(Now, 5, Lock);
        Assert.True(user.IsLockedOut(Now));
        Assert.True(user.IsLockedOut(Now + Lock - TimeSpan.FromSeconds(1)));
        Assert.False(user.IsLockedOut(Now + Lock));

        var until = user.LockedUntil;
        user.RegisterFailedSignIn(Now + TimeSpan.FromMinutes(5), 5, Lock);
        Assert.Equal(until, user.LockedUntil);
    }

    [Fact]
    public void A_successful_sign_in_resets_the_count_and_clears_a_lock()
    {
        var user = WithSignIn();
        for (var i = 0; i < 5; i++)
        {
            user.RegisterFailedSignIn(Now, 5, Lock);
        }

        user.RegisterSuccessfulSignIn();

        Assert.False(user.IsLockedOut(Now));
        Assert.Equal(0, user.FailedSignIns);
    }
}
