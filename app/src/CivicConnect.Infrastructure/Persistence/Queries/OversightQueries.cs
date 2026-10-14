using System.Linq.Expressions;
using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence.Queries;

/// <summary>
/// FR-015 to FR-018 and FR-023. Every figure is counted from the request rows
/// with the same filter, so the counts, the performance table, the list and the
/// export always agree (NFR-008). Overdue is decided against one clock reading
/// per call, so a request cannot be counted overdue in one figure and not another.
/// </summary>
public sealed class OversightQueries(CivicConnectDbContext db, IClock clock) : IOversightQueries
{
    private const int RecentRows = 50;

    // The same rule as ResponseTarget.IsOpen, written so EF can translate it.
    private static readonly Expression<Func<Request, bool>> IsOpen = r =>
        r.Status == RequestStatus.Received || r.Status == RequestStatus.Assigned || r.Status == RequestStatus.InProgress;

    public async Task<OversightSummary> SummaryAsync(OversightFilter filter, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var requests = Filtered(filter, now);

        var counted = await requests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Every status is listed, so a zero is visible rather than missing.
        var byStatus = Enum.GetValues<RequestStatus>()
            .Select(s => new StatusCount(s, counted.FirstOrDefault(c => c.Status == s)?.Count ?? 0))
            .ToList();

        var perCategory = await requests
            .GroupBy(r => r.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Count = g.Count(),
                Open = g.Count(r =>
                    r.Status == RequestStatus.Received || r.Status == RequestStatus.Assigned || r.Status == RequestStatus.InProgress),
                Overdue = g.Count(r =>
                    (r.Status == RequestStatus.Received || r.Status == RequestStatus.Assigned || r.Status == RequestStatus.InProgress)
                    && r.TargetResponseAtUtc < now),
                Resolved = g.Count(r => r.Status == RequestStatus.Resolved || r.Status == RequestStatus.Closed)
            })
            .ToListAsync(ct);

        // FR-018: when each request was first marked Resolved, for the time to
        // resolve and whether it met its target. Only three columns are read.
        var resolutions = await (
                from h in db.RequestHistory.AsNoTracking()
                where h.Action == "StatusChanged" && h.NewValue == nameof(RequestStatus.Resolved)
                join r in requests on h.RequestId equals r.Id
                select new { r.CategoryId, r.CreatedAtUtc, r.TargetResponseAtUtc, ResolvedAt = h.OccurredAtUtc })
            .ToListAsync(ct);
        var resolvedBy = resolutions.ToLookup(x => x.CategoryId);

        var names = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var byCategory = perCategory
            .Select(c => new CategoryCount(
                c.CategoryId,
                names.GetValueOrDefault(c.CategoryId, c.CategoryId.ToString()),
                c.Count,
                c.Open,
                c.Overdue,
                c.Resolved,
                resolvedBy[c.CategoryId].Count(x => x.ResolvedAt <= x.TargetResponseAtUtc),
                AverageHours(resolvedBy[c.CategoryId].Select(x => x.ResolvedAt - x.CreatedAtUtc))))
            .OrderBy(c => c.Category)
            .ToList();

        var recent = await Rows(requests, now).Take(RecentRows).ToListAsync(ct);

        return new OversightSummary(
            byStatus.Sum(s => s.Count),
            byCategory.Sum(c => c.Open),
            byCategory.Sum(c => c.Overdue),
            byCategory.Sum(c => c.Resolved),
            resolutions.Count(x => x.ResolvedAt <= x.TargetResponseAtUtc),
            AverageHours(resolutions.Select(x => x.ResolvedAt - x.CreatedAtUtc)),
            byStatus,
            byCategory,
            recent);
    }

    public async Task<IReadOnlyList<OversightRow>> RowsAsync(OversightFilter filter, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return await Rows(Filtered(filter, now), now).Take(IOversightQueries.ExportMax).ToListAsync(ct);
    }

    private IQueryable<Request> Filtered(OversightFilter filter, DateTimeOffset now)
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

        // FR-016 (AC-016.1, AC-016.2).
        if (filter.Lifecycle is Lifecycle.Open or Lifecycle.Overdue)
        {
            requests = requests.Where(IsOpen);
        }

        if (filter.Lifecycle == Lifecycle.Overdue)
        {
            requests = requests.Where(r => r.TargetResponseAtUtc < now);
        }

        return requests;
    }

    private IQueryable<OversightRow> Rows(IQueryable<Request> requests, DateTimeOffset now)
        => requests
            .Join(db.Categories, r => r.CategoryId, c => c.Id, (r, c) => new { Request = r, Category = c.Name })
            .OrderByDescending(x => x.Request.CreatedAtUtc)
            .ThenBy(x => x.Request.Reference)
            .Select(x => new OversightRow(
                x.Request.Id,
                x.Request.Reference,
                x.Request.Title,
                x.Category,
                x.Request.Status,
                x.Request.CreatedAtUtc,
                x.Request.TargetResponseAtUtc,
                (x.Request.Status == RequestStatus.Received
                 || x.Request.Status == RequestStatus.Assigned
                 || x.Request.Status == RequestStatus.InProgress)
                && x.Request.TargetResponseAtUtc < now));

    private static double? AverageHours(IEnumerable<TimeSpan> durations)
    {
        var hours = durations.Select(d => d.TotalHours).ToList();
        return hours.Count == 0 ? null : Math.Round(hours.Average(), 1);
    }
}
