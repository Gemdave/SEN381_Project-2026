using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Abstractions;

public sealed record OversightFilter(RequestStatus? Status = null, Guid? CategoryId = null);

public sealed record StatusCount(RequestStatus Status, int Count);

public sealed record CategoryCount(Guid CategoryId, string Category, int Count);

public sealed record OversightRow(
    Guid Id, string Reference, string Title, string Category, RequestStatus Status, DateTimeOffset CreatedAtUtc);

public sealed record OversightSummary(
    int Total, IReadOnlyList<StatusCount> ByStatus, IReadOnlyList<CategoryCount> ByCategory, IReadOnlyList<OversightRow> Recent);

/// <summary>
/// FR-015 and FR-017. Counts by status and by category, and the newest matching
/// requests, read straight from the request records so the figures reconcile
/// with them (NFR-008). Callers check MayViewOversightAsync first.
/// </summary>
public interface IOversightQueries
{
    Task<OversightSummary> SummaryAsync(OversightFilter filter, CancellationToken ct);
}
