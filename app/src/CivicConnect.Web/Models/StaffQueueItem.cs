using CivicConnect.Domain.Requests;

namespace CivicConnect.Web.Models;

public sealed record StaffQueueItem(
    Guid Id,
    string Reference,
    string Title,
    RequestStatus Status,
    string Category,
    DateTimeOffset CreatedAtUtc,
    bool IsAssigned);
