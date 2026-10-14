using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Abstractions;

/// <summary>FR-016 (AC-016.1). Open is Received, Assigned or In progress; overdue is open and past its target.</summary>
public enum Lifecycle
{
    Open,
    Overdue
}

public sealed record OversightFilter(RequestStatus? Status = null, Guid? CategoryId = null, Lifecycle? Lifecycle = null);

public sealed record StatusCount(RequestStatus Status, int Count);

/// <summary>
/// FR-017 and FR-018 (AC-018.1). Volumes, open, overdue and resolved counts for
/// one category. Resolved counts Resolved and Closed requests; the average and
/// the within-target count come from when each was first marked Resolved.
/// </summary>
public sealed record CategoryCount(
    Guid CategoryId,
    string Category,
    int Count,
    int Open,
    int Overdue,
    int Resolved,
    int ResolvedWithinTarget,
    double? AverageHoursToResolve);

public sealed record OversightRow(
    Guid Id,
    string Reference,
    string Title,
    string Category,
    RequestStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset TargetResponseAtUtc,
    bool Overdue);

public sealed record OversightSummary(
    int Total,
    int Open,
    int Overdue,
    int Resolved,
    int ResolvedWithinTarget,
    double? AverageHoursToResolve,
    IReadOnlyList<StatusCount> ByStatus,
    IReadOnlyList<CategoryCount> ByCategory,
    IReadOnlyList<OversightRow> Recent);

/// <summary>
/// FR-015 to FR-018 and FR-023. Counts by status and by category, service
/// performance figures and the matching requests, all read straight from the
/// request records with one filter so the figures reconcile with them and with
/// each other (NFR-008). Callers check the policy first.
/// </summary>
public interface IOversightQueries
{
    /// <summary>How many rows an export holds at most.</summary>
    const int ExportMax = 10000;

    Task<OversightSummary> SummaryAsync(OversightFilter filter, CancellationToken ct);

    /// <summary>FR-018 (AC-018.2). Every matching request, newest first, for export.</summary>
    Task<IReadOnlyList<OversightRow>> RowsAsync(OversightFilter filter, CancellationToken ct);
}
