using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Abstractions;

public sealed record CategoryOption(Guid Id, string Name);

public sealed record RequestListItem(
    Guid Id, string Reference, string Title, string CategoryName, RequestStatus Status, DateTimeOffset CreatedAtUtc);

public sealed record RequestDetail(
    Guid Id,
    string Reference,
    string Title,
    string Description,
    string Location,
    string CategoryName,
    string RequesterName,
    RequestStatus Status,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// The read side for pages (ADR-004). These queries only shape data; the caller
/// asks IAuthorisationPolicy first, except where the query is itself scoped to
/// the caller (FR-004).
/// </summary>
public interface IRequestQueries
{
    /// <summary>FR-002. Read on every use, so a new category needs no code change (AC-021.1).</summary>
    Task<IReadOnlyList<CategoryOption>> ListActiveCategoriesAsync(CancellationToken ct);

    Task<bool> IsActiveCategoryAsync(Guid categoryId, CancellationToken ct);

    /// <summary>FR-004. Filtered on the requester inside the query, so other rows are never loaded (NFR-003).</summary>
    Task<IReadOnlyList<RequestListItem>> ListForRequesterAsync(Guid requesterId, CancellationToken ct);

    /// <summary>FR-003 and FR-010 (AC-010.1).</summary>
    Task<RequestDetail?> GetDetailAsync(Guid requestId, CancellationToken ct);
}
