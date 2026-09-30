using CivicConnect.Core.Abstractions;
using Npgsql;

namespace CivicConnect.Data;

public class NpgsqlUnitOfWorkFactory(NpgsqlDataSource dataSource) : IUnitOfWorkFactory
{
    public async Task<IUnitOfWork> BeginAsync(CancellationToken ct = default)
    {
        var conn = await dataSource.OpenConnectionAsync(ct);
        var tx = await conn.BeginTransactionAsync(ct);
        return new NpgsqlUnitOfWork(conn, tx);
    }
}

// One connection, one transaction, shared by every repository below.
// If CommitAsync is never called, DisposeAsync rolls everything back.
public sealed class NpgsqlUnitOfWork : IUnitOfWork
{
    private readonly NpgsqlConnection _conn;
    private readonly NpgsqlTransaction _tx;
    private bool _committed;

    internal NpgsqlUnitOfWork(NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        _conn = conn;
        _tx = tx;
        Requests = new RequestRepository(conn, tx);
        History = new HistoryRepository(conn, tx);
        Notes = new NoteRepository(conn, tx);
        Feedback = new FeedbackRepository(conn, tx);
        Categories = new CategoryRepository(conn, tx);
    }

    public IRequestRepository Requests { get; }
    public IHistoryRepository History { get; }
    public INoteRepository Notes { get; }
    public IFeedbackRepository Feedback { get; }
    public ICategoryRepository Categories { get; }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        await _tx.CommitAsync(ct);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
            await _tx.RollbackAsync();
        await _tx.DisposeAsync();
        await _conn.DisposeAsync();
    }
}
