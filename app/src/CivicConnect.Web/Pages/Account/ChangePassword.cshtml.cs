using CivicConnect.Application.Access;
using CivicConnect.Web.Models;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace CivicConnect.Web.Pages.Account;

/// <summary>
/// CR-003. A signed-in user chooses their own password. It is the first thing a user
/// must do after an administrator sets or resets their password, and the only page
/// open to them until they have. Other sessions end when the password changes; this
/// one is signed in again with the new security stamp.
/// </summary>
public class ChangePasswordModel(
    SignInService authentication,
    ICurrentUser currentUser,
    IWebHostEnvironment environment) : PageModel
{
    [BindProperty] public string? CurrentPassword { get; set; }

    [BindProperty] public string? NewPassword { get; set; }

    [BindProperty] public string? ConfirmPassword { get; set; }

    public PageMessage? Message { get; private set; }

    public bool MustChange => User.HasClaim(SignInSetup.MustChangeClaim, "true");

    public IActionResult OnGet() => environment.IsDevelopment() ? NotFound() : Page();

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (environment.IsDevelopment())
        {
            return NotFound();
        }

        if (NewPassword != ConfirmPassword)
        {
            Message = PageMessage.Error("The new password and its confirmation do not match.");
            return Page();
        }

        var result = await authentication.ChangePasswordAsync(currentUser.Id, CurrentPassword, NewPassword, ct);

        if (result.Outcome != ChangePasswordOutcome.Done)
        {
            Message = PageMessage.Error(result.Outcome switch
            {
                ChangePasswordOutcome.WrongCurrentPassword => "The current password is incorrect.",
                ChangePasswordOutcome.WeakPassword => PasswordPolicy.Check(NewPassword, null) ?? "Do not use your email address as your password.",
                ChangePasswordOutcome.SameAsCurrent => "Choose a password different from the current one.",
                _ => "Your account cannot change its password."
            });
            return Page();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            SignInSetup.PrincipalFor(
                currentUser.Id, User.FindFirstValue(ClaimTypes.Name) ?? string.Empty, result.SecurityStamp, mustChangePassword: false),
            new AuthenticationProperties { IsPersistent = false });

        return Redirect("/Home");
    }
}
