using CivicConnect.Domain.Audit;

namespace CivicConnect.Application.Abstractions;

/// <summary>What the audit trail can be narrowed to (AC-022.1).</summary>
public enum AuditKind
{
    All,
    Requests,
    Categories
}

/// <summary>One line of the audit trail, from the request history or from an audit entry.</summary>
public sealed record AuditItem(
    DateTimeOffset OccurredAtUtc,
    string Action,
    string Subject,
    string? OldValue,
    string? NewValue,
    string? Detail,
    Guid ActorUserId);

/// <summary>
/// FR-022 and NFR-007. Insert only: there is no update or delete. The trail
/// combines request history (status changes, assignments, notes) with audit
/// entries (category changes), newest first.
/// </summary>
public interface IAuditRepository
{
    void Add(AuditEntry entry);

    Task<IReadOnlyList<AuditItem>> RecentAsync(AuditKind kind, int take, CancellationToken ct);
}
