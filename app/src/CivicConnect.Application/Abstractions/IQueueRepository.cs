using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Abstractions;

public enum QueueSort
{
    Oldest,
    Newest,
    Reference,
    Status
}

public sealed record QueueFilter(
    RequestStatus? Status = null,
    Guid? CategoryId = null,
    string? Search = null,
    bool MineOnly = false,
    QueueSort Sort = QueueSort.Oldest,
    int Page = 1,
    int PageSize = 20);

public sealed record QueueRow(
    Guid Id,
    string Reference,
    string Title,
    RequestStatus Status,
    string Category,
    DateTimeOffset CreatedAtUtc,
    bool IsAssigned);

public sealed record CategoryOption(Guid Id, string Name);

public interface IQueueRepository
{
    Task<bool> IsServiceStaffAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// FR-008 to FR-009. Staff see unassigned requests and their own, never
    /// requests owned by someone else.
    /// </summary>
    Task<IReadOnlyList<QueueRow>> SearchAsync(Guid staffUserId, QueueFilter filter, CancellationToken ct);

    Task<IReadOnlyList<CategoryOption>> CategoriesAsync(CancellationToken ct);
}
