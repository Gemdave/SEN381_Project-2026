using CivicConnect.Core.Models;
using CivicConnect.Core.Rules;

namespace CivicConnect.Web.Dtos;

// What comes in.
public record SubmitRequestBody(string? Title, string? Description, string? Location, short CategoryId);

public record StatusChangeBody(RequestStatus Status, string? Reason);

public record NoteBody(string? Body);

// What goes out. Kept separate from the Core models so the API can stay stable
// even if the internals change.
public record RequestView(
    Guid Id,
    string Reference,
    string Title,
    string Description,
    string Location,
    short CategoryId,
    string CategoryName,
    RequestStatus Status,
    string StatusText,
    Guid? AssignedTo,
    DateTimeOffset? AssignedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static RequestView From(ServiceRequest r) => new(
        r.Id, r.Reference, r.Title, r.Description, r.Location, r.CategoryId, r.CategoryName,
        r.Status, StatusRules.Display(r.Status), r.AssignedTo, r.AssignedAt, r.CreatedAt, r.UpdatedAt);
}

public record RequestDetailView(
    RequestView Request,
    IReadOnlyList<HistoryEntry>? History,
    IReadOnlyList<RequestNote>? Notes);

public record PageView<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public record CategoryView(short Id, string Name);
