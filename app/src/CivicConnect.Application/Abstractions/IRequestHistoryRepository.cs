using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Abstractions;

public interface IRequestHistoryRepository
{
    /// <summary>Insert only. There is deliberately no update or delete here (NFR-007).</summary>
    void Add(RequestHistoryEntry entry);

    Task<IReadOnlyList<RequestHistoryEntry>> ForRequestAsync(Guid requestId, CancellationToken ct);
}
