using CivicConnect.Application.Access;
using CivicConnect.Web.Models;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Account;

/// <summary>
/// CR-003. Email and password sign-in. It exists outside Development only; in
/// Development the site root offers the stand-in user chooser instead (RSK-012).
/// The same message is shown for an unknown email, a wrong password and a deactivated
/// account (AC-019.1), and the return address must be local so the form cannot be
/// used to send someone to another site.
/// </summary>
[AllowAnonymous]
public class SignInModel(SignInService authentication, IWebHostEnvironment environment) : PageModel
{
    [BindProperty] public string? Email { get; set; }

    [BindProperty] public string? Password { get; set; }

    public string? ReturnUrl { get; private set; }

    public PageMessage? Message { get; private set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (environment.IsDevelopment())
        {
            return Redirect("/");
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect("/Home");
        }

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl, CancellationToken ct)
    {
        if (environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await authentication.SignInAsync(Email, Password, ct);

        if (result.Outcome == SignInOutcome.Succeeded)
        {
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                SignInSetup.PrincipalFor(result.UserId, result.DisplayName, result.SecurityStamp, result.MustChangePassword),
                new AuthenticationProperties { IsPersistent = false });

            if (result.MustChangePassword)
            {
                return Redirect(SignInSetup.ChangePasswordPath);
            }

            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Home");
        }

        ReturnUrl = returnUrl;
        Password = null;
        Message = PageMessage.Error(result.Outcome == SignInOutcome.LockedOut
            ? $"Too many failed attempts. Try again in {SignInService.LockoutDuration.TotalMinutes:0} minutes, or ask an administrator to reset your password."
            : "The email or password is incorrect.");
        return Page();
    }
}
