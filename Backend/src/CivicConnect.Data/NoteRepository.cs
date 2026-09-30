using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using Npgsql;

namespace CivicConnect.Data;

internal class NoteRepository(NpgsqlConnection conn, NpgsqlTransaction tx) : INoteRepository
{
    public async Task<RequestNote> AddAsync(Guid requestId, Guid authorId, string body, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"INSERT INTO request_note (request_id, author_id, body)
              VALUES (@request, @author, @body)
              RETURNING id, created_at",
            ("request", requestId), ("author", authorId), ("body", body));

        await using var r = await cmd.ExecuteReaderAsync(ct);
        await r.ReadAsync(ct);
        return new RequestNote(r.GetInt64(0), requestId, authorId, body, r.GetFieldValue<DateTimeOffset>(1));
    }

    public async Task<IReadOnlyList<RequestNote>> ListAsync(Guid requestId, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"SELECT id, request_id, author_id, body, created_at
                FROM request_note WHERE request_id = @request ORDER BY created_at, id",
            ("request", requestId));

        var list = new List<RequestNote>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(new RequestNote(r.GetInt64(0), r.GetGuid(1), r.GetGuid(2), r.GetString(3),
                r.GetFieldValue<DateTimeOffset>(4)));
        return list;
    }
}
