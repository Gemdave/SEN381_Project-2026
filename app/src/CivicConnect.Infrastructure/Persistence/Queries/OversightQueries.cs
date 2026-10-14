using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Queries;

/// <summary>
/// FR-015 and FR-017. Every figure is counted from the request rows with the
/// same filter, so the counts and the list always agree (NFR-008).
/// </summary>
public sealed class OversightQueries(CivicConnectDbContext db) : IOversightQueries
{
    private const int RecentRows = 50;

    public async Task<OversightSummary> SummaryAsync(OversightFilter filter, CancellationToken ct)
    {
        var requests = db.Requests.AsNoTracking();
        if (filter.Status is { } status)
        {
            requests = requests.Where(r => r.Status == status);
        }

        if (filter.CategoryId is { } categoryId)
        {
            requests = requests.Where(r => r.CategoryId == categoryId);
        }

        var counted = await requests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Every status is listed, so a zero is visible rather than missing.
        var byStatus = Enum.GetValues<RequestStatus>()
            .Select(s => new StatusCount(s, counted.FirstOrDefault(c => c.Status == s)?.Count ?? 0))
            .ToList();

        var byCategory = (await requests
                .Join(db.Categories, r => r.CategoryId, c => c.Id, (r, c) => new { c.Id, c.Name })
                .GroupBy(x => new { x.Id, x.Name })
                .Select(g => new { g.Key.Id, g.Key.Name, Count = g.Count() })
                .ToListAsync(ct))
            .OrderBy(x => x.Name)
            .Select(x => new CategoryCount(x.Id, x.Name, x.Count))
            .ToList();

        var recent = await requests
            .Join(db.Categories, r => r.CategoryId, c => c.Id, (r, c) => new { Request = r, Category = c.Name })
            .OrderByDescending(x => x.Request.CreatedAtUtc)
            .ThenBy(x => x.Request.Reference)
            .Take(RecentRows)
            .Select(x => new OversightRow(
                x.Request.Id, x.Request.Reference, x.Request.Title, x.Category, x.Request.Status, x.Request.CreatedAtUtc))
            .ToListAsync(ct);

        return new OversightSummary(byStatus.Sum(s => s.Count), byStatus, byCategory, recent);
    }
}
