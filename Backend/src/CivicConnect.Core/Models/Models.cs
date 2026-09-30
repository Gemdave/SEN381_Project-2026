namespace CivicConnect.Core.Models;

public enum RequestStatus
{
    Received,
    Assigned,
    InProgress,
    Resolved,
    Closed,
    Rejected
}

public record Category(short Id, string Name, bool IsActive);

public record ServiceRequest(
    Guid Id,
    string Reference,
    string Title,
    string Description,
    string Location,
    short CategoryId,
    string CategoryName,
    Guid RequesterId,
    RequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? AssignedTo,
    DateTimeOffset? AssignedAt,
    int Version);

public record HistoryEntry(
    long Id,
    Guid RequestId,
    string Action,
    RequestStatus? FromStatus,
    RequestStatus ToStatus,
    Guid ActorId,
    string? Note,
    DateTimeOffset CreatedAt);

public record RequestNote(long Id, Guid RequestId, Guid AuthorId, string Body, DateTimeOffset CreatedAt);

public record FeedbackItem(
    long Id,
    Guid RequestId,
    Guid RecipientId,
    string Kind,
    string Message,
    string? Reason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public record CurrentUser(Guid Id, string DisplayName, IReadOnlySet<string> Permissions);

public record NewRequest(string Title, string Description, string Location, short CategoryId);

public record QueueFilter(RequestStatus? Status, short? CategoryId, string Sort, int Page, int PageSize);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

// History and notes are only filled in for staff.
public record RequestDetail(
    ServiceRequest Request,
    IReadOnlyList<HistoryEntry>? History,
    IReadOnlyList<RequestNote>? Notes);

public static class Permissions
{
    public const string Submit = "request.submit";
    public const string ViewOwn = "request.view_own";
    public const string Queue = "request.queue";
    public const string ViewDetail = "request.view_detail";
    public const string Assign = "request.assign";
    public const string UpdateStatus = "request.update_status";
    public const string Close = "request.close";
    public const string AddNote = "request.add_note";
}

public static class FeedbackKinds
{
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string Updated = "Updated";
    public const string Completed = "Completed";
}
