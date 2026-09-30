using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;
using Npgsql;

namespace CivicConnect.Data;

public class UserRepository(NpgsqlDataSource dataSource) : IUserRepository
{
    // Deactivated users come back as null, so they cannot get in (AC-019.1).
    public async Task<CurrentUser?> FindActiveByEmailAsync(string email, CancellationToken ct)
    {
        await using var cmd = dataSource.CreateCommand(
            @"SELECT u.id, u.display_name,
                     coalesce(array_agg(DISTINCT p.code) FILTER (WHERE p.code IS NOT NULL), '{}')
                FROM app_user u
                LEFT JOIN user_role ur       ON ur.user_id = u.id
                LEFT JOIN role_permission rp ON rp.role_id = ur.role_id
                LEFT JOIN permission p       ON p.id = rp.permission_id
               WHERE lower(u.email) = lower(@email) AND u.is_active
               GROUP BY u.id, u.display_name");
        cmd.Parameters.AddWithValue("email", email);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return null;

        var permissions = r.GetFieldValue<string[]>(2).ToHashSet();
        return new CurrentUser(r.GetGuid(0), r.GetString(1), permissions);
    }
}
