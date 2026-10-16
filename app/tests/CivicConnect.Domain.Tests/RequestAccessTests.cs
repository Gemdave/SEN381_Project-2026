using CivicConnect.Domain.Access;
using Xunit;

namespace CivicConnect.Domain.Tests;

/// <summary>
/// UT-04 decision table for the rules beside the role rules (ADR-005): who may
/// submit (FR-001) and the FR-008 category scope (AC-008.1, AC-008.2, NFR-006).
/// </summary>
[Trait("TestCase", "UT-04")]
public class RequestAccessTests
{
    [Theory]
    [InlineData(UserRole.Requester, true)]
    [InlineData(UserRole.ServiceStaff, false)]
    [InlineData(UserRole.Management, false)]
    [InlineData(UserRole.Administrator, false)]
    [InlineData(UserRole.Sponsor, false)]
    public void Only_requesters_may_submit(UserRole role, bool expected)
        => Assert.Equal(expected, RequestAccess.MaySubmit(role));

    [Theory]
    [InlineData(UserRole.ServiceStaff, true, true)]
    [InlineData(UserRole.ServiceStaff, false, false)]
    [InlineData(UserRole.Requester, false, true)]
    [InlineData(UserRole.Management, false, true)]
    [InlineData(UserRole.Administrator, false, true)]
    public void Only_staff_are_limited_to_their_category_scope(UserRole role, bool inScope, bool expected)
        => Assert.Equal(expected, RequestAccess.WithinScope(role, inScope));

    [Theory]
    [InlineData(UserRole.Requester, false)]
    [InlineData(UserRole.ServiceStaff, false)]
    [InlineData(UserRole.Management, true)]
    [InlineData(UserRole.Administrator, true)]
    [InlineData(UserRole.Sponsor, false)]
    public void Only_managers_and_administrators_see_oversight(UserRole role, bool expected)
        => Assert.Equal(expected, RequestAccess.MayViewOversight(role));

    [Theory]
    [InlineData(UserRole.Requester, false)]
    [InlineData(UserRole.ServiceStaff, false)]
    [InlineData(UserRole.Management, false)]
    [InlineData(UserRole.Administrator, true)]
    [InlineData(UserRole.Sponsor, false)]
    public void Only_administrators_administer(UserRole role, bool expected)
        => Assert.Equal(expected, RequestAccess.MayAdminister(role));

    [Theory]
    [Trait("TestCase", "FR-023")]
    [InlineData(UserRole.Requester, false)]
    [InlineData(UserRole.ServiceStaff, false)]
    [InlineData(UserRole.Management, true)]
    [InlineData(UserRole.Administrator, true)]
    [InlineData(UserRole.Sponsor, true)]
    public void Sponsors_managers_and_administrators_see_the_summary(UserRole role, bool expected)
        => Assert.Equal(expected, RequestAccess.MayViewSummary(role));

    [Theory]
    [Trait("TestCase", "FR-006")]
    [InlineData(UserRole.Requester, true, false, true)]
    [InlineData(UserRole.Requester, false, false, false)]
    [InlineData(UserRole.ServiceStaff, false, true, true)]
    [InlineData(UserRole.ServiceStaff, false, false, false)]
    [InlineData(UserRole.Management, false, false, false)]
    [InlineData(UserRole.Administrator, false, false, false)]
    [InlineData(UserRole.Sponsor, false, false, false)]
    public void The_requester_or_the_owning_staff_member_may_attach(UserRole role, bool isRequester, bool isOwner, bool expected)
        => Assert.Equal(expected, RequestAccess.MayAttach(role, isRequester, isOwner));
}
