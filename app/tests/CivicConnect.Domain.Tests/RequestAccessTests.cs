using CivicConnect.Domain.Access;
using Xunit;

namespace CivicConnect.Domain.Tests;

/// <summary>
/// UT-04 decision table for ADR-005, extended with the FR-008 category scope:
/// AC-008.1, AC-008.2, AC-004.2, AC-014.2, NFR-003, NFR-006.
/// </summary>
[Trait("TestCase", "UT-04")]
public class RequestAccessTests
{
    private static readonly Guid Me = Guid.NewGuid();
    private static readonly Guid Someone = Guid.NewGuid();

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Staff_see_a_request_only_inside_their_category_scope(bool inScope, bool expected)
        => Assert.Equal(expected, RequestAccess.MayView(UserRole.ServiceStaff, Me, Someone, inScope));

    [Fact]
    public void A_requester_sees_their_own_request_but_not_another_requesters()
    {
        Assert.True(RequestAccess.MayView(UserRole.Requester, Me, Me, inScope: false));
        Assert.False(RequestAccess.MayView(UserRole.Requester, Me, Someone, inScope: false));
    }

    [Theory]
    [InlineData(UserRole.ServiceStaff, true, true)]
    [InlineData(UserRole.ServiceStaff, false, false)]
    [InlineData(UserRole.Requester, true, false)]
    [InlineData(UserRole.Management, true, false)]
    public void Only_in_scope_staff_may_accept(UserRole role, bool inScope, bool expected)
        => Assert.Equal(expected, RequestAccess.MayAssign(role, inScope));

    [Fact]
    public void Only_the_in_scope_owner_may_move_or_close_a_request()
    {
        Assert.True(RequestAccess.MayWorkOn(UserRole.ServiceStaff, Me, ownerId: Me, inScope: true));
        Assert.False(RequestAccess.MayWorkOn(UserRole.ServiceStaff, Me, ownerId: Someone, inScope: true));
        Assert.False(RequestAccess.MayWorkOn(UserRole.ServiceStaff, Me, ownerId: Me, inScope: false));
        Assert.False(RequestAccess.MayWorkOn(UserRole.Requester, Me, ownerId: Me, inScope: true));
    }
}
