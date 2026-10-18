using System.Security.Claims;
using CivicConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Web.Security;

/// <summary>
/// CR-003. Cookie sign-in for every environment except Development (which keeps the
/// stand-in the test suite depends on). Everything is private by default: the fallback
/// policy refuses anyone who is not signed in, and only the sign-in page, the error
/// page and the site root opt out with [AllowAnonymous].
/// </summary>
public static class SignInSetup
{
    public const string StampClaim = "cc:stamp";
    public const string MustChangeClaim = "cc:must-change";
    public const string SignInPath = "/Account/SignIn";
    public const string ChangePasswordPath = "/Account/ChangePassword";
    public const string SignOutPath = "/Account/SignOut";

    public static IServiceCollection AddCivicConnectSignIn(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "cc.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.LoginPath = SignInPath;
                options.AccessDeniedPath = "/Home";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;

                // Pages send the browser to the sign-in page; the API answers with a status code instead.
                options.Events.OnRedirectToLogin = context =>
                {
                    if (IsApi(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }

                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (IsApi(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }

                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = ValidateAsync;
            });

        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddScoped<ICurrentUser, CookieCurrentUser>();
        return services;
    }

    /// <summary>
    /// After an administrator sets or resets a password, the user may do nothing but
    /// choose their own. This runs after authentication and before authorisation.
    /// </summary>
    public static IApplicationBuilder UseMustChangePassword(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            if (context.User.Identity?.IsAuthenticated == true
                && context.User.HasClaim(MustChangeClaim, "true")
                && !path.StartsWithSegments(ChangePasswordPath, StringComparison.OrdinalIgnoreCase)
                && !path.StartsWithSegments(SignOutPath, StringComparison.OrdinalIgnoreCase))
            {
                if (IsApi(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                }
                else
                {
                    context.Response.Redirect(ChangePasswordPath);
                }

                return;
            }

            await next();
        });

    public static ClaimsPrincipal PrincipalFor(Guid userId, string displayName, Guid securityStamp, bool mustChangePassword)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, displayName),
            new(StampClaim, securityStamp.ToString())
        };

        if (mustChangePassword)
        {
            claims.Add(new Claim(MustChangeClaim, "true"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private static bool IsApi(HttpRequest request)
        => request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Runs on every request that carries the cookie. A user who was deactivated
    /// (AC-019.1), or whose password was reset or changed, loses the session at once
    /// instead of keeping it until the cookie expires.
    /// </summary>
    private static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var validId = Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var validStamp = Guid.TryParse(principal?.FindFirstValue(StampClaim), out var stamp);

        var current = validId && validStamp
            ? await context.HttpContext.RequestServices.GetRequiredService<CivicConnectDbContext>()
                .Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsActive, u.SecurityStamp })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted)
            : null;

        if (current is not { IsActive: true } || current.SecurityStamp != stamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
