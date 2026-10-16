using CivicConnect.Domain.ReferenceData;
using CivicConnect.Domain.Requests;
using Xunit;

namespace CivicConnect.Domain.Tests;

/// <summary>FR-016 (AC-016.1, AC-016.2) and CR-005: open, overdue and the target response time.</summary>
[Trait("TestCase", "FR-016")]
public class ResponseTargetTests
{
    private static readonly DateTimeOffset Due = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(RequestStatus.Received, true)]
    [InlineData(RequestStatus.Assigned, true)]
    [InlineData(RequestStatus.InProgress, true)]
    [InlineData(RequestStatus.Resolved, false)]
    [InlineData(RequestStatus.Closed, false)]
    [InlineData(RequestStatus.Rejected, false)]
    public void Open_means_staff_still_owe_an_outcome(RequestStatus status, bool open)
        => Assert.Equal(open, ResponseTarget.IsOpen(status));

    [Fact]
    public void An_open_request_is_overdue_only_after_its_due_time()
    {
        Assert.False(ResponseTarget.IsOverdue(RequestStatus.InProgress, Due, Due.AddMinutes(-1)));
        Assert.False(ResponseTarget.IsOverdue(RequestStatus.InProgress, Due, Due));
        Assert.True(ResponseTarget.IsOverdue(RequestStatus.InProgress, Due, Due.AddMinutes(1)));
    }

    [Theory]
    [InlineData(RequestStatus.Resolved)]
    [InlineData(RequestStatus.Closed)]
    [InlineData(RequestStatus.Rejected)]
    public void A_finished_request_is_never_overdue(RequestStatus status)
        => Assert.False(ResponseTarget.IsOverdue(status, Due, Due.AddDays(30)));

    [Fact]
    public void A_request_takes_its_due_time_from_the_target_when_submitted()
    {
        var submitted = Due.AddHours(-48);
        var request = Request.Submit("REQ-1", "Title", "Description", Guid.NewGuid(), Guid.NewGuid(), submitted, "Here", 24);

        Assert.Equal(submitted.AddHours(24), request.TargetResponseAtUtc);
    }

    [Fact]
    public void Without_a_target_the_default_of_72_hours_applies()
    {
        var request = Request.Submit("REQ-1", "Title", "Description", Guid.NewGuid(), Guid.NewGuid(), Due);

        Assert.Equal(Due.AddHours(ResponseTarget.DefaultHours), request.TargetResponseAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(ResponseTarget.MaxHours + 1)]
    public void A_category_refuses_a_target_outside_the_range(int hours)
    {
        var category = Category.Create("Fault");

        Assert.Throws<ArgumentOutOfRangeException>(() => category.SetTarget(hours));
        Assert.Equal(ResponseTarget.DefaultHours, category.TargetResponseHours);
    }

    [Fact]
    public void A_category_keeps_a_valid_target()
    {
        var category = Category.Create("Fault");

        category.SetTarget(24);

        Assert.Equal(24, category.TargetResponseHours);
    }
}
