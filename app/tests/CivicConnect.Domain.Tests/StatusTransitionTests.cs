using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.Domain.Tests;

public class StatusTransitionTests
{
    [Theory]
    [InlineData(RequestStatus.Received, RequestStatus.Assigned)]
    [InlineData(RequestStatus.Assigned, RequestStatus.InProgress)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Resolved)]
    [InlineData(RequestStatus.Resolved, RequestStatus.Closed)]
    public void Allows_the_baselined_lifecycle(RequestStatus from, RequestStatus to)
        => Assert.True(StatusTransition.IsAllowed(from, to));

    [Theory]
    [InlineData(RequestStatus.Received, RequestStatus.Closed)]
    [InlineData(RequestStatus.Received, RequestStatus.Resolved)]
    [InlineData(RequestStatus.Closed, RequestStatus.Assigned)]
    [InlineData(RequestStatus.Assigned, RequestStatus.Received)]
    public void Refuses_anything_outside_it(RequestStatus from, RequestStatus to)
        => Assert.False(StatusTransition.IsAllowed(from, to));
}
