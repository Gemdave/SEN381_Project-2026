using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Web.Security;

/// <summary>
/// Stands in for authentication while CR-003 is open (RSK-012). It reads a user
/// id from the query string and refuses to work outside Development, so it
/// cannot survive a deployment by accident.
/// </summary>
public sealed class DevelopmentCurrentUser : ICurrentUser
{
    private readonly Guid id;

    public DevelopmentCurrentUser(
        IHttpContextAccessor accessor,
        IWebHostEnvironment environment,
        CivicConnectDbContext db)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "DevelopmentCurrentUser is Development only. Replace it when CR-003 closes.");
        }

        var raw = accessor.HttpContext?.Request.Query["user"].ToString();
        if (Guid.TryParse(raw, out var parsed))
        {
            id = parsed;
            return;
        }

        // Fall back to the first seeded staff member so the pages are usable.
        id = db.Users
            .Where(u => u.Role == CivicConnect.Domain.Access.UserRole.ServiceStaff)
            .Select(u => u.Id)
            .FirstOrDefault();
    }

    public Guid Id => id;
}
