using Npgsql;

namespace CivicConnect.Data;

internal static class Sql
{
    // Every query in this project goes through here so values are always parameters,
    // never pasted into the SQL text.
    public static NpgsqlCommand Command(
        NpgsqlConnection conn, NpgsqlTransaction tx, string sql, params (string name, object? value)[] args)
    {
        var cmd = new NpgsqlCommand(sql, conn, tx);
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }
}
