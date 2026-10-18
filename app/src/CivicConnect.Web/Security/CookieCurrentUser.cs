using System.Security.Claims;

namespace CivicConnect.Web.Security;

/// <summary>
/// CR-003. The identity of the signed-in user, taken from the authentication cookie
/// (the same ICurrentUser the development stand-in provides, so no page, endpoint
/// or policy changed). Nobody signed in is Guid.Empty, which the policy refuses
/// everything (AC-019.1). The cookie carries only the user id: the role is read from
/// the database on each action, so a role change applies at once (AC-020.1).
/// </summary>
public sealed class CookieCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid Id { get; } = Parse(accessor.HttpContext?.User);

    private static Guid Parse(ClaimsPrincipal? principal)
        => Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
