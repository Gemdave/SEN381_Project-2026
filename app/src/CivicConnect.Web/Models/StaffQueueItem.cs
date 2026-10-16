using CivicConnect.Domain.Requests;

namespace CivicConnect.Web.Models;

/// <summary>One row of the staff queue. Overdue is decided once, when the page loads (FR-016).</summary>
public sealed record StaffQueueItem(
    Guid Id,
    string Reference,
    string Title,
    RequestStatus Status,
    string Category,
    DateTimeOffset CreatedAtUtc,
    bool IsAssigned,
    DateTimeOffset TargetResponseAtUtc,
    bool Overdue);
