using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;

namespace CivicConnect.Web.UI;

/// <summary>
/// PLACEHOLDER, exactly like CurrentUserAccessor and for the same reason: how
/// people really sign in is still open under CR-003 and ADR-005. The API reads
/// a header, which a browser cannot send, so the pages read a cookie set on the
/// landing page from the seeded development users. Outside Development it
/// returns nobody, so it cannot quietly become a way in (RSK-012).
/// </summary>
public sealed class PageUserAccessor(IUserRepository users, IWebHostEnvironment env)
{
    public const string CookieName = "civicconnect-dev-user";

    public bool DevelopmentSignInAvailable => env.IsDevelopment();

    public async Task<CurrentUser?> TryGetAsync(HttpContext http, CancellationToken ct)
    {
        if (!env.IsDevelopment())
        {
            return null;
        }

        var email = http.Request.Cookies[CookieName];
        return string.IsNullOrWhiteSpace(email)
            ? null
            : await users.FindActiveByEmailAsync(email, ct);
    }
}
