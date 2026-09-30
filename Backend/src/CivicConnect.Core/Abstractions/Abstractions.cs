using CivicConnect.Core.Models;

namespace CivicConnect.Core.Abstractions;

// One transaction. Dispose without committing and everything is rolled back.
public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> BeginAsync(CancellationToken ct = default);
}

public interface IUnitOfWork : IAsyncDisposable
{
    IRequestRepository Requests { get; }
    IHistoryRepository History { get; }
    INoteRepository Notes { get; }
    IFeedbackRepository Feedback { get; }
    ICategoryRepository Categories { get; }

    Task CommitAsync(CancellationToken ct = default);
}

public interface IRequestRepository
{
    Task<ServiceRequest> InsertAsync(Guid requesterId, NewRequest input, CancellationToken ct);
    Task<ServiceRequest?> FindAsync(Guid id, CancellationToken ct);
    Task<PagedResult<ServiceRequest>> ListForRequesterAsync(
        Guid requesterId, RequestStatus? status, int page, int pageSize, CancellationToken ct);
    Task<PagedResult<ServiceRequest>> ListQueueAsync(Guid staffId, QueueFilter filter, CancellationToken ct);

    // Both return false when someone else got there first (0 rows changed).
    Task<bool> TryAssignAsync(Guid id, Guid staffId, CancellationToken ct);
    Task<bool> TryChangeStatusAsync(Guid id, RequestStatus expectedCurrent, RequestStatus next, CancellationToken ct);
}

public interface IHistoryRepository
{
    Task AddAsync(Guid requestId, string action, RequestStatus? from, RequestStatus to,
        Guid actorId, string? note, CancellationToken ct);
    Task<IReadOnlyList<HistoryEntry>> ListAsync(Guid requestId, CancellationToken ct);
}

public interface INoteRepository
{
    Task<RequestNote> AddAsync(Guid requestId, Guid authorId, string body, CancellationToken ct);
    Task<IReadOnlyList<RequestNote>> ListAsync(Guid requestId, CancellationToken ct);
}

public interface IFeedbackRepository
{
    Task AddAsync(Guid requestId, Guid recipientId, string kind, string message,
        string? reason, CancellationToken ct);
    Task<IReadOnlyList<FeedbackItem>> ListForRecipientAsync(Guid recipientId, CancellationToken ct);
    Task<bool> MarkReadAsync(long id, Guid recipientId, CancellationToken ct);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken ct);
    Task<Category?> FindAsync(short id, CancellationToken ct);
}

// Lives outside the unit of work: it is only used to work out who is calling.
public interface IUserRepository
{
    Task<CurrentUser?> FindActiveByEmailAsync(string email, CancellationToken ct);
}
