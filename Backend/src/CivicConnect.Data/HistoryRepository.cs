using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using Npgsql;

namespace CivicConnect.Data;

// Insert and read only. There is deliberately no update or delete method (NFR-007).
internal class HistoryRepository(NpgsqlConnection conn, NpgsqlTransaction tx) : IHistoryRepository
{
    public async Task AddAsync(Guid requestId, string action, RequestStatus? from, RequestStatus to,
        Guid actorId, string? note, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"INSERT INTO request_status_history (request_id, action, from_status, to_status, actor_id, note)
              VALUES (@request, @action, @from, @to, @actor, @note)",
            ("request", requestId), ("action", action), ("from", from?.ToString()),
            ("to", to.ToString()), ("actor", actorId), ("note", note));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<HistoryEntry>> ListAsync(Guid requestId, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"SELECT id, request_id, action, from_status, to_status, actor_id, note, created_at
                FROM request_status_history
               WHERE request_id = @request
               ORDER BY created_at, id",
            ("request", requestId));

        var list = new List<HistoryEntry>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new HistoryEntry(
                r.GetInt64(0), r.GetGuid(1), r.GetString(2),
                r.IsDBNull(3) ? null : Enum.Parse<RequestStatus>(r.GetString(3)),
                Enum.Parse<RequestStatus>(r.GetString(4)),
                r.GetGuid(5),
                r.IsDBNull(6) ? null : r.GetString(6),
                r.GetFieldValue<DateTimeOffset>(7)));
        }
        return list;
    }
}
