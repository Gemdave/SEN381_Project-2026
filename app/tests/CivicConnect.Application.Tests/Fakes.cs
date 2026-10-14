using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Tests;

internal sealed class FakeRequestRepository(int rowsAffected, RequestOwnership? ownership) : IRequestRepository
{
    private long nextReference = 1000;

    public List<Request> Added { get; } = [];

    public Task<int> TryAssignAsync(Guid requestId, Guid staffUserId, DateTimeOffset now, CancellationToken ct)
        => Task.FromResult(rowsAffected);

    public Task<int> TryTransitionAsync(Guid requestId, RequestStatus from, RequestStatus to, CancellationToken ct)
        => Task.FromResult(rowsAffected);

    public Task<RequestOwnership?> GetOwnershipAsync(Guid requestId, CancellationToken ct)
        => Task.FromResult(ownership);

    public Task<long> NextReferenceNumberAsync(CancellationToken ct) => Task.FromResult(nextReference++);

    public Task AddAsync(Request request, CancellationToken ct)
    {
        Added.Add(request);
        return Task.CompletedTask;
    }
}

internal sealed class FakeHistoryRepository : IRequestHistoryRepository
{
    public List<RequestHistoryEntry> Entries { get; } = [];

    public void Add(RequestHistoryEntry entry) => Entries.Add(entry);

    public Task<IReadOnlyList<RequestHistoryEntry>> ForRequestAsync(Guid requestId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<RequestHistoryEntry>>(Entries);
}

internal sealed class FakePolicy(bool permitted) : IAuthorisationPolicy
{
    public List<RequestAction> Asked { get; } = [];

    public Task<bool> MayAsync(Guid userId, RequestAction action, Guid requestId, CancellationToken ct)
    {
        Asked.Add(action);
        return Task.FromResult(permitted);
    }

    public Task<bool> MaySubmitAsync(Guid userId, CancellationToken ct) => Task.FromResult(permitted);
}

internal sealed class FakeQueries(params Guid[] activeCategories) : IRequestQueries
{
    public Task<IReadOnlyList<CategoryOption>> ListActiveCategoriesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<CategoryOption>>(
            activeCategories.Select(id => new CategoryOption(id, "Category")).ToList());

    public Task<bool> IsActiveCategoryAsync(Guid categoryId, CancellationToken ct)
        => Task.FromResult(activeCategories.Contains(categoryId));

    public Task<IReadOnlyList<RequestListItem>> ListForRequesterAsync(
        Guid requesterId, RequestStatus? status, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<RequestListItem>>([]);

    public Task<RequestDetail?> GetDetailAsync(Guid requestId, CancellationToken ct)
        => Task.FromResult<RequestDetail?>(null);

    public Task<IReadOnlyDictionary<Guid, string>> DisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
        => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
}

internal sealed class DirectUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct)
        => work(ct);

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; } = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
}
