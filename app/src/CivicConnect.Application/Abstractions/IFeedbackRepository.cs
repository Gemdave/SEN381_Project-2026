using CivicConnect.Domain.Requests;

namespace CivicConnect.Application.Abstractions;

public interface IFeedbackRepository
{
    void Add(RequesterFeedback feedback);

    Task<IReadOnlyList<RequesterFeedback>> ForRequestAsync(Guid requestId, CancellationToken ct);
}
