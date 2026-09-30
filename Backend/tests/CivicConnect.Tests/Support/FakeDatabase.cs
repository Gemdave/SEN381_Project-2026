using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;

namespace CivicConnect.Tests.Support;

// A small in-memory stand-in for PostgreSQL so the service rules can be tested
// without a database. It copies the two important behaviours of the real one:
// the conditional updates only succeed once, and history is only ever added to.
public class FakeDatabase : IUnitOfWorkFactory
{
    public Dictionary<Guid, ServiceRequest> Requests { get; } = new();
    public List<HistoryEntry> History { get; } = new();
    public List<RequestNote> Notes { get; } = new();
    public List<FeedbackItem> Feedback { get; } = new();
    public List<Category> Categories { get; } =
    [
        new Category(1, "Fault", true),
        new Category(2, "IT", true),
        new Category(3, "Retired category", false)
    ];

    internal readonly object Gate = new();
    private int _nextReference = 1;
    private long _nextId = 1;

    internal int NextReference() => _nextReference++;
    internal long NextId() => _nextId++;

    public Task<IUnitOfWork> BeginAsync(CancellationToken ct = default) =>
        Task.FromResult<IUnitOfWork>(new FakeUnitOfWork(this));
}

internal class FakeUnitOfWork(FakeDatabase db) : IUnitOfWork
{
    public IRequestRepository Requests { get; } = new FakeRequests(db);
    public IHistoryRepository History { get; } = new FakeHistory(db);
    public INoteRepository Notes { get; } = new FakeNotes(db);
    public IFeedbackRepository Feedback { get; } = new FakeFeedback(db);
    public ICategoryRepository Categories { get; } = new FakeCategories(db);

    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal class FakeRequests(FakeDatabase db) : IRequestRepository
{
    public Task<ServiceRequest> InsertAsync(Guid requesterId, NewRequest input, CancellationToken ct)
    {
        lock (db.Gate)
        {
            var category = db.Categories.Single(c => c.Id == input.CategoryId);
            var now = DateTimeOffset.UtcNow;
            var request = new ServiceRequest(
                Guid.NewGuid(), $"CC-2026-{db.NextReference():D6}", input.Title, input.Description,
                input.Location, category.Id, category.Name, requesterId, RequestStatus.Received,
                now, now, null, null, 1);
            db.Requests[request.Id] = request;
            return Task.FromResult(request);
        }
    }

    public Task<ServiceRequest?> FindAsync(Guid id, CancellationToken ct)
    {
        lock (db.Gate)
            return Task.FromResult(db.Requests.GetValueOrDefault(id));
    }

    public Task<PagedResult<ServiceRequest>> ListForRequesterAsync(
        Guid requesterId, RequestStatus? status, int page, int pageSize, CancellationToken ct)
    {
        lock (db.Gate)
        {
            var all = db.Requests.Values
                .Where(r => r.RequesterId == requesterId && (status is null || r.Status == status))
                .OrderByDescending(r => r.CreatedAt).ToList();
            return Task.FromResult(Page(all, page, pageSize));
        }
    }

    public Task<PagedResult<ServiceRequest>> ListQueueAsync(Guid staffId, QueueFilter f, CancellationToken ct)
    {
        lock (db.Gate)
        {
            var all = db.Requests.Values
                .Where(r => r.AssignedTo is null || r.AssignedTo == staffId)
                .Where(r => f.Status is null
                    ? r.Status is not (RequestStatus.Closed or RequestStatus.Rejected)
                    : r.Status == f.Status)
                .Where(r => f.CategoryId is null || r.CategoryId == f.CategoryId)
                .OrderBy(r => r.CreatedAt).ToList();
            return Task.FromResult(Page(all, f.Page, f.PageSize));
        }
    }

    public Task<bool> TryAssignAsync(Guid id, Guid staffId, CancellationToken ct)
    {
        lock (db.Gate)
        {
            var r = db.Requests[id];
            if (r.AssignedTo is not null || r.Status != RequestStatus.Received)
                return Task.FromResult(false);

            db.Requests[id] = r with
            {
                AssignedTo = staffId, AssignedAt = DateTimeOffset.UtcNow,
                Status = RequestStatus.Assigned, Version = r.Version + 1
            };
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryChangeStatusAsync(Guid id, RequestStatus expectedCurrent, RequestStatus next, CancellationToken ct)
    {
        lock (db.Gate)
        {
            var r = db.Requests[id];
            if (r.Status != expectedCurrent)
                return Task.FromResult(false);

            db.Requests[id] = r with { Status = next, Version = r.Version + 1 };
            return Task.FromResult(true);
        }
    }

    private static PagedResult<ServiceRequest> Page(List<ServiceRequest> all, int page, int pageSize) =>
        new(all.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, all.Count);
}

internal class FakeHistory(FakeDatabase db) : IHistoryRepository
{
    public Task AddAsync(Guid requestId, string action, RequestStatus? from, RequestStatus to,
        Guid actorId, string? note, CancellationToken ct)
    {
        lock (db.Gate)
            db.History.Add(new HistoryEntry(db.NextId(), requestId, action, from, to, actorId, note, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<HistoryEntry>> ListAsync(Guid requestId, CancellationToken ct)
    {
        lock (db.Gate)
            return Task.FromResult<IReadOnlyList<HistoryEntry>>(db.History.Where(h => h.RequestId == requestId).ToList());
    }
}

internal class FakeNotes(FakeDatabase db) : INoteRepository
{
    public Task<RequestNote> AddAsync(Guid requestId, Guid authorId, string body, CancellationToken ct)
    {
        lock (db.Gate)
        {
            var note = new RequestNote(db.NextId(), requestId, authorId, body, DateTimeOffset.UtcNow);
            db.Notes.Add(note);
            return Task.FromResult(note);
        }
    }

    public Task<IReadOnlyList<RequestNote>> ListAsync(Guid requestId, CancellationToken ct)
    {
        lock (db.Gate)
            return Task.FromResult<IReadOnlyList<RequestNote>>(db.Notes.Where(n => n.RequestId == requestId).ToList());
    }
}

internal class FakeFeedback(FakeDatabase db) : IFeedbackRepository
{
    public Task AddAsync(Guid requestId, Guid recipientId, string kind, string message, string? reason, CancellationToken ct)
    {
        lock (db.Gate)
            db.Feedback.Add(new FeedbackItem(db.NextId(), requestId, recipientId, kind, message, reason, DateTimeOffset.UtcNow, null));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FeedbackItem>> ListForRecipientAsync(Guid recipientId, CancellationToken ct)
    {
        lock (db.Gate)
            return Task.FromResult<IReadOnlyList<FeedbackItem>>(db.Feedback.Where(f => f.RecipientId == recipientId).ToList());
    }

    public Task<bool> MarkReadAsync(long id, Guid recipientId, CancellationToken ct)
    {
        lock (db.Gate)
        {
            var index = db.Feedback.FindIndex(f => f.Id == id && f.RecipientId == recipientId);
            if (index < 0) return Task.FromResult(false);
            db.Feedback[index] = db.Feedback[index] with { ReadAt = DateTimeOffset.UtcNow };
            return Task.FromResult(true);
        }
    }
}

internal class FakeCategories(FakeDatabase db) : ICategoryRepository
{
    public Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Category>>(db.Categories.Where(c => c.IsActive).ToList());

    public Task<Category?> FindAsync(short id, CancellationToken ct) =>
        Task.FromResult(db.Categories.FirstOrDefault(c => c.Id == id));
}
