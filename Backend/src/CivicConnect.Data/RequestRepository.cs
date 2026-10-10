using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using Npgsql;

namespace CivicConnect.Data;

internal class RequestRepository(NpgsqlConnection conn, NpgsqlTransaction tx) : IRequestRepository
{
    private const string Columns = @"
        r.id, r.reference, r.title, r.description, r.location, r.category_id, c.name,
        r.requester_id, r.status, r.created_at, r.updated_at, r.assigned_to, r.assigned_at, r.version";

    private const string From = " FROM request r JOIN category c ON c.id = r.category_id ";

    public async Task<ServiceRequest> InsertAsync(Guid requesterId, NewRequest input, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"INSERT INTO request (title, description, location, category_id, requester_id)
              VALUES (@title, @description, @location, @category, @requester)
              RETURNING id",
            ("title", input.Title), ("description", input.Description), ("location", input.Location),
            ("category", input.CategoryId), ("requester", requesterId));

        var id = (Guid)(await cmd.ExecuteScalarAsync(ct))!;
        return (await FindAsync(id, ct))!;
    }

    public async Task<ServiceRequest?> FindAsync(Guid id, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            "SELECT " + Columns + From + "WHERE r.id = @id", ("id", id));
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    public Task<PagedResult<ServiceRequest>> ListForRequesterAsync(
        Guid requesterId, RequestStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var args = new List<(string, object?)>
        {
            ("requester", requesterId),
            ("status", status?.ToString())
        };
        // requester_id is always in the WHERE clause, so there is no way to list someone else's.
        var where = "WHERE r.requester_id = @requester AND (@status::text IS NULL OR r.status = @status)";
        return ListAsync(where, args, "r.created_at DESC", page, pageSize, ct);
    }

    public Task<PagedResult<ServiceRequest>> ListQueueAsync(Guid staffId, QueueFilter filter, CancellationToken ct)
    {
        var args = new List<(string, object?)>
        {
            ("staff", staffId),
            ("status", filter.Status?.ToString()),
            ("category", filter.CategoryId)
        };

        // Unowned requests plus the caller's own. With no status filter, finished work is hidden.
        var where = @"WHERE (r.assigned_to IS NULL OR r.assigned_to = @staff)
                      AND (@status::text IS NULL OR r.status = @status)
                      AND (@status::text IS NOT NULL OR r.status NOT IN ('Closed', 'Rejected'))
                      AND (@category::smallint IS NULL OR r.category_id = @category)";

        // The sort value was already checked against a fixed list; this maps it to SQL text
        // we wrote ourselves, so user input never reaches the ORDER BY.
        var orderBy = filter.Sort switch
        {
            "-created" => "r.created_at DESC",
            "status" => "r.status, r.created_at",
            "category" => "c.name, r.created_at",
            _ => "r.created_at ASC"
        };

        return ListAsync(where, args, orderBy, filter.Page, filter.PageSize, ct);
    }

    // Claim an unowned request. Returns false if someone else got there first.
    public async Task<bool> TryAssignAsync(Guid id, Guid staffId, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"UPDATE request
                 SET assigned_to = @staff, assigned_at = now(), status = 'Assigned',
                     version = version + 1, updated_at = now()
               WHERE id = @id AND assigned_to IS NULL AND status = 'Received'",
            ("id", id), ("staff", staffId));
        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // Only changes the status if it is still what the caller last saw.
    public async Task<bool> TryChangeStatusAsync(
        Guid id, RequestStatus expectedCurrent, RequestStatus next, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"UPDATE request
                 SET status = @next, version = version + 1, updated_at = now()
               WHERE id = @id AND status = @current",
            ("id", id), ("current", expectedCurrent.ToString()), ("next", next.ToString()));
        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    private async Task<PagedResult<ServiceRequest>> ListAsync(
        string where, List<(string, object?)> args, string orderBy, int page, int pageSize, CancellationToken ct)
    {
        var argArray = args.Select(a => (a.Item1, a.Item2)).ToArray();

        await using var countCmd = Sql.Command(conn, tx, "SELECT count(*)" + From + where, argArray);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        var pageArgs = argArray.Concat(new (string, object?)[]
        {
            ("limit", pageSize),
            ("offset", (page - 1) * pageSize)
        }).ToArray();

        await using var cmd = Sql.Command(conn, tx,
            "SELECT " + Columns + From + where + " ORDER BY " + orderBy + " LIMIT @limit OFFSET @offset",
            pageArgs);

        var items = new List<ServiceRequest>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            items.Add(Read(reader));

        return new PagedResult<ServiceRequest>(items, page, pageSize, total);
    }

    private static ServiceRequest Read(NpgsqlDataReader r) => new(
        Id: r.GetGuid(0),
        Reference: r.GetString(1),
        Title: r.GetString(2),
        Description: r.GetString(3),
        Location: r.GetString(4),
        CategoryId: r.GetInt16(5),
        CategoryName: r.GetString(6),
        RequesterId: r.GetGuid(7),
        Status: Enum.Parse<RequestStatus>(r.GetString(8)),
        CreatedAt: r.GetFieldValue<DateTimeOffset>(9),
        UpdatedAt: r.GetFieldValue<DateTimeOffset>(10),
        AssignedTo: r.IsDBNull(11) ? null : r.GetGuid(11),
        AssignedAt: r.IsDBNull(12) ? null : r.GetFieldValue<DateTimeOffset>(12),
        Version: r.GetInt32(13));
}
