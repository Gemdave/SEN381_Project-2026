using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Web.Security;

/// <summary>
/// Stands in for authentication while CR-003 is open (RSK-012). The user is
/// chosen on the sign-in page at the site root (or with ?user= in a link, which
/// the tests use) and kept in a cookie. It refuses to work outside Development,
/// so it cannot survive a deployment by accident. A browser that has chosen
/// nobody has no identity, and so does one signed in as a user who has since
/// been deactivated: the policy refuses everything either asks (AC-019.1).
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
            && db.Users.Any(u => u.Id == chosen && u.IsActive))
        {
            SignIn(http!.Response, chosen);
            id = chosen;
            return;
        }

        // An unknown id (for example after the database was recreated) or a
        // deactivated user leaves the browser with no identity, never someone else's.
        if (Guid.TryParse(http?.Request.Cookies[CookieName], out var remembered)
            && db.Users.Any(u => u.Id == remembered && u.IsActive))
        {
            id = remembered;
        }
    }

    public Guid Id => id;

    /// <summary>Remembers the chosen user for this browser. The caller has checked they are active.</summary>
    public static void SignIn(HttpResponse response, Guid userId)
        => response.Cookies.Append(CookieName, userId.ToString(),
            new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });

    public static void SignOut(HttpResponse response) => response.Cookies.Delete(CookieName);
}
