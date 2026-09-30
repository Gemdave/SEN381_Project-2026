using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;

namespace CivicConnect.Tests;

public class StatusRulesTests
{
    [Theory]
    [InlineData(RequestStatus.Received, RequestStatus.Assigned)]
    [InlineData(RequestStatus.Received, RequestStatus.Rejected)]
    [InlineData(RequestStatus.Assigned, RequestStatus.InProgress)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Resolved)]
    [InlineData(RequestStatus.Resolved, RequestStatus.Closed)]
    public void Allowed_moves_are_allowed(RequestStatus from, RequestStatus to) =>
        Assert.True(StatusRules.CanMove(from, to));

    [Theory]
    [InlineData(RequestStatus.Received, RequestStatus.Closed)]
    [InlineData(RequestStatus.Received, RequestStatus.Resolved)]
    [InlineData(RequestStatus.Assigned, RequestStatus.Closed)]
    [InlineData(RequestStatus.Resolved, RequestStatus.InProgress)]
    [InlineData(RequestStatus.Closed, RequestStatus.Received)]
    [InlineData(RequestStatus.Rejected, RequestStatus.Assigned)]
    [InlineData(RequestStatus.Assigned, RequestStatus.Rejected)]
    public void Everything_else_is_refused(RequestStatus from, RequestStatus to) =>
        Assert.False(StatusRules.CanMove(from, to));

    [Fact]
    public void Display_text_is_plain_language() =>
        Assert.Equal("In Progress", StatusRules.Display(RequestStatus.InProgress));
}
