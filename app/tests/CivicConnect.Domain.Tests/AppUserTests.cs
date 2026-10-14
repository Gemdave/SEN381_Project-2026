using CivicConnect.Domain.Access;
using Xunit;

namespace CivicConnect.Domain.Tests;

/// <summary>FR-019 (AC-019.1) and FR-020 (AC-020.1): accounts are deactivated, never deleted, and roles are data.</summary>
[Trait("TestCase", "FR-019")]
public class AppUserTests
{
    [Fact]
    public void A_new_user_is_active_with_the_given_role()
    {
        var user = AppUser.Create("  New Staff  ", UserRole.ServiceStaff);

        Assert.True(user.IsActive);
        Assert.Equal(UserRole.ServiceStaff, user.Role);
        Assert.Equal("New Staff", user.DisplayName);
    }

    [Fact]
    public void A_user_needs_a_name()
        => Assert.Throws<ArgumentException>(() => AppUser.Create(" ", UserRole.Requester));

    [Fact]
    public void Deactivating_keeps_the_account_and_reactivating_restores_it()
    {
        var user = AppUser.Create("Someone", UserRole.Requester);
        var id = user.Id;

        user.Deactivate();
        Assert.False(user.IsActive);
        Assert.Equal(id, user.Id);

        user.Reactivate();
        Assert.True(user.IsActive);
    }

    [Fact]
    [Trait("TestCase", "FR-020")]
    public void Changing_the_role_replaces_it()
    {
        var user = AppUser.Create("Someone", UserRole.Requester);

        user.ChangeRole(UserRole.Management);

        Assert.Equal(UserRole.Management, user.Role);
    }
}
