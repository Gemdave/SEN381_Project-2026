namespace CivicConnect.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>
    /// Runs the work inside one database transaction, so a status change can
    /// never be saved without its history row (RSK-009).
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
