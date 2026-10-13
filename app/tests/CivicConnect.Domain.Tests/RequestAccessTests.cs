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
}
