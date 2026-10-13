using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Repositories;

public sealed class FeedbackRepository(CivicConnectDbContext db) : IFeedbackRepository
{
    public void Add(RequesterFeedback feedback) => db.Feedback.Add(feedback);

    public async Task<IReadOnlyList<RequesterFeedback>> ForRequestAsync(Guid requestId, CancellationToken ct)
        => await db.Feedback
            .AsNoTracking()
            .Where(f => f.RequestId == requestId)
            .OrderBy(f => f.CreatedAtUtc)
            .ToListAsync(ct);
}
