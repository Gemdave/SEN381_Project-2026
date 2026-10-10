using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using Npgsql;

namespace CivicConnect.Data;

internal class FeedbackRepository(NpgsqlConnection conn, NpgsqlTransaction tx) : IFeedbackRepository
{
    public async Task AddAsync(Guid requestId, Guid recipientId, string kind, string message,
        string? reason, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"INSERT INTO feedback_item (request_id, recipient_id, kind, message, reason)
              VALUES (@request, @recipient, @kind, @message, @reason)",
            ("request", requestId), ("recipient", recipientId), ("kind", kind),
            ("message", message), ("reason", reason));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<FeedbackItem>> ListForRecipientAsync(Guid recipientId, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"SELECT id, request_id, recipient_id, kind, message, reason, created_at, read_at
                FROM feedback_item
               WHERE recipient_id = @recipient
               ORDER BY created_at DESC, id DESC
               LIMIT 100",
            ("recipient", recipientId));

        var list = new List<FeedbackItem>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new FeedbackItem(
                r.GetInt64(0), r.GetGuid(1), r.GetGuid(2), r.GetString(3), r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5),
                r.GetFieldValue<DateTimeOffset>(6),
                r.IsDBNull(7) ? null : r.GetFieldValue<DateTimeOffset>(7)));
        }
        return list;
    }

    public async Task<bool> MarkReadAsync(long id, Guid recipientId, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            @"UPDATE feedback_item SET read_at = coalesce(read_at, now())
               WHERE id = @id AND recipient_id = @recipient",
            ("id", id), ("recipient", recipientId));
        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }
}
