using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Web.Security;

/// <summary>
/// Stands in for authentication while CR-003 is open (RSK-012). It reads a user
/// id from the query string and refuses to work outside Development, so it
/// cannot survive a deployment by accident. The choice is kept in a cookie, so
/// links and form posts without ?user= stay with the same user.
/// </summary>
public sealed class DevelopmentCurrentUser : ICurrentUser
{
    public const string CookieName = "cc-dev-user";

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

        var http = accessor.HttpContext;
        if (Guid.TryParse(http?.Request.Query["user"].ToString(), out var chosen)
            && db.Users.Any(u => u.Id == chosen))
        {
            http!.Response.Cookies.Append(CookieName, chosen.ToString(),
                new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });
            id = chosen;
            return;
        }

        // An unknown id (for example after the database was recreated) is ignored.
        if (Guid.TryParse(http?.Request.Cookies[CookieName], out var remembered)
            && db.Users.Any(u => u.Id == remembered))
        {
            id = remembered;
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
