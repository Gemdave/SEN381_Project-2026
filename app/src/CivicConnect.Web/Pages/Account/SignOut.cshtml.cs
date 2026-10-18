using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Account;

/// <summary>CR-003. Signing out is a POST, so a link on another site cannot sign someone out.</summary>
public class SignOutModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Home");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/Account/SignIn");
    }
}
