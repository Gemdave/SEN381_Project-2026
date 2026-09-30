using CivicConnect.Web.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages;

/// <summary>
/// Landing page. It also carries the development sign in, because the API reads
/// an X-Dev-User header that a browser cannot send. Replaced when CR-003 closes.
/// </summary>
public class IndexModel(PageUserAccessor pageUser) : PageModel
{
    public string? SignedInAs { get; private set; }

    public bool DevelopmentSignInAvailable => pageUser.DevelopmentSignInAvailable;

    public string[] SeededUsers { get; } =
    [
        "requester1@civicconnect.test",
        "requester2@civicconnect.test",
        "staff1@civicconnect.test",
        "staff2@civicconnect.test"
    ];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var user = await pageUser.TryGetAsync(HttpContext, ct);
        SignedInAs = user?.DisplayName;
    }

    public IActionResult OnPostSignIn(string email)
    {
        if (!DevelopmentSignInAvailable || !SeededUsers.Contains(email))
        {
            return RedirectToPage("/Index");
        }

        Response.Cookies.Append(PageUserAccessor.CookieName, email, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true
        });

        return RedirectToPage("/Index");
    }

    public IActionResult OnPostSignOut()
    {
        Response.Cookies.Delete(PageUserAccessor.CookieName);
        return RedirectToPage("/Index");
    }
}
