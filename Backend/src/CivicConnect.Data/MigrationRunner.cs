using System.Reflection;
using Npgsql;

namespace CivicConnect.Data;

// Applies the numbered .sql files once, in order, and remembers which ones ran.
public static class MigrationRunner
{
    public static async Task<int> RunAsync(NpgsqlDataSource dataSource, CancellationToken ct = default)
    {
        await using (var setup = dataSource.CreateCommand(
            @"CREATE TABLE IF NOT EXISTS schema_migration (
                name       text PRIMARY KEY,
                applied_at timestamptz NOT NULL DEFAULT now())"))
        {
            await setup.ExecuteNonQueryAsync(ct);
        }

        var assembly = Assembly.GetExecutingAssembly();
        var scripts = assembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var applied = 0;
        foreach (var resource in scripts)
        {
            var name = resource[(resource.IndexOf("Migrations.", StringComparison.Ordinal) + "Migrations.".Length)..];

            await using var conn = await dataSource.OpenConnectionAsync(ct);

            await using (var check = new NpgsqlCommand("SELECT 1 FROM schema_migration WHERE name = @name", conn))
            {
                check.Parameters.AddWithValue("name", name);
                if (await check.ExecuteScalarAsync(ct) is not null)
                    continue;
            }

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            var script = await reader.ReadToEndAsync(ct);

            // The script and its bookkeeping row succeed or fail together.
            await using var tx = await conn.BeginTransactionAsync(ct);
            await using (var run = new NpgsqlCommand(script, conn, tx))
                await run.ExecuteNonQueryAsync(ct);
            await using (var record = new NpgsqlCommand("INSERT INTO schema_migration (name) VALUES (@name)", conn, tx))
            {
                record.Parameters.AddWithValue("name", name);
                await record.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct);
            applied++;
        }
        return applied;
    }
}
