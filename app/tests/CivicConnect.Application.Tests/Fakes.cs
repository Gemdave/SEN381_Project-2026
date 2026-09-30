using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Tests;

internal sealed class FakeRequestRepository(int rowsAffected, RequestOwnership? ownership) : IRequestRepository
{
    public Task<int> TryAssignAsync(Guid requestId, Guid staffUserId, DateTimeOffset now, CancellationToken ct)
        => Task.FromResult(rowsAffected);

    public Task<int> TryTransitionAsync(Guid requestId, RequestStatus from, RequestStatus to, CancellationToken ct)
        => Task.FromResult(rowsAffected);

    public Task<RequestOwnership?> GetOwnershipAsync(Guid requestId, CancellationToken ct)
        => Task.FromResult(ownership);

    public Task AddAsync(Request request, CancellationToken ct) => Task.CompletedTask;
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
    public Task<bool> MayAsync(Guid userId, RequestAction action, Guid requestId, CancellationToken ct)
        => Task.FromResult(permitted);
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
