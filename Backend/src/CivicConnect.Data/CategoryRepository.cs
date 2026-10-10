using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using Npgsql;

namespace CivicConnect.Data;

internal class CategoryRepository(NpgsqlConnection conn, NpgsqlTransaction tx) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            "SELECT id, name, is_active FROM category WHERE is_active ORDER BY name");

        var list = new List<Category>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(new Category(r.GetInt16(0), r.GetString(1), r.GetBoolean(2)));
        return list;
    }

    public async Task<Category?> FindAsync(short id, CancellationToken ct)
    {
        await using var cmd = Sql.Command(conn, tx,
            "SELECT id, name, is_active FROM category WHERE id = @id", ("id", id));
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new Category(r.GetInt16(0), r.GetString(1), r.GetBoolean(2)) : null;
    }
}
