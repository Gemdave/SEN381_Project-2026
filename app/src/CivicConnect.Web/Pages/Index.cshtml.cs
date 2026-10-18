using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Web.Models;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages;

/// <summary>
/// The page the site opens on: choose who to sign in as, grouped by role. Opening
/// it always starts from nobody, so the site never lands as a remembered user.
/// This is the Development stand-in for sign-in while CR-003 is open (RSK-012),
/// so it does not exist anywhere else. Only active users are offered or accepted
/// (AC-019.1). Staff are shown with their categories, because those decide which
/// requests they see (FR-008).
/// </summary>
[AllowAnonymous]
public class IndexModel(
    IUserRepository users,
    ICategoryRepository categories,
    IWebHostEnvironment environment) : PageModel
{
    public sealed record Choice(Guid Id, string Name, string? Detail);

    public sealed record RoleGroup(UserRole Role, string Title, string Description, IReadOnlyList<Choice> Users);

    public IReadOnlyList<RoleGroup> Groups { get; private set; } = [];
    public PageMessage? Message { get; private set; }

    public static string Describe(UserRole role) => role switch
    {
        UserRole.Requester => "Submit requests and follow your own through to a fix.",
        UserRole.ServiceStaff => "Work the queue for your categories: accept, update, resolve or reject.",
        UserRole.Management => "Oversight of all requests, overdue work and service performance.",
        UserRole.Administrator => "Users, roles, categories and the audit trail.",
        UserRole.Sponsor => "The high-level service summary.",
        _ => string.Empty
    };

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!environment.IsDevelopment())
        {
            return RedirectToPage("/Account/SignIn");
        }

        // A remembered user is signed out first, and the page reloads so that
        // nothing on it, the header included, still shows them.
        if (Request.Cookies.ContainsKey(DevelopmentCurrentUser.CookieName))
        {
            DevelopmentCurrentUser.SignOut(Response);
            return Redirect("/");
        }

        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid userId, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var user = await users.FindAsync(userId, ct);
        if (user is not { IsActive: true })
        {
            Message = PageMessage.Error("That user cannot sign in. Choose someone from the list.");
            await LoadAsync(ct);
            return Page();
        }

        DevelopmentCurrentUser.SignIn(Response, user.Id);
        return Redirect("/Home");
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var active = (await users.ListAsync(ct)).Where(u => u.IsActive).ToList();
        var names = (await categories.ListAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name);

        Groups = Enum.GetValues<UserRole>()
            .Select(role => new RoleGroup(
                role,
                Admin.UsersModel.RoleText(role),
                Describe(role),
                active.Where(u => u.Role == role)
                    .OrderBy(u => u.DisplayName)
                    .Select(u => new Choice(u.Id, u.DisplayName, role == UserRole.ServiceStaff ? Categories(u, names) : null))
                    .ToList()))
            .Where(g => g.Users.Count > 0)
            .ToList();
    }

    private static string Categories(UserRow user, IReadOnlyDictionary<Guid, string> names)
    {
        var list = user.ScopedCategoryIds.Where(names.ContainsKey).Select(id => names[id]).Order().ToList();
        return list.Count == 0 ? "No categories yet" : string.Join(", ", list);
    }
}
