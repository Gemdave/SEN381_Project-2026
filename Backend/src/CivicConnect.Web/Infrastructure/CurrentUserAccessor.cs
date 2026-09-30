using CivicConnect.Core.Abstractions;
using CivicConnect.Core.Models;

namespace CivicConnect.Web.Infrastructure;

// PLACEHOLDER until ADR-005 / CR-003 decides how people really sign in.
// In Development it trusts an X-Dev-User header holding an email address from db/dev_seed.sql.
// Anywhere else it refuses everyone, so this can never quietly become a back door.
public class CurrentUserAccessor(IUserRepository users, IWebHostEnvironment env)
{
    public async Task<CurrentUser> GetAsync(HttpContext http, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            throw new UnauthenticatedException("Sign-in is not available yet.");

        var email = http.Request.Headers["X-Dev-User"].ToString();
        if (string.IsNullOrWhiteSpace(email))
            throw new UnauthenticatedException("Please sign in.");

        return await users.FindActiveByEmailAsync(email, ct)
            ?? throw new UnauthenticatedException("Please sign in.");
    }
}
