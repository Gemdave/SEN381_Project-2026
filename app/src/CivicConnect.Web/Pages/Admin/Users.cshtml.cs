using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Access;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Web.Models;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Admin;

/// <summary>
/// FR-019 (AC-019.1) and FR-020 (AC-020.1, AC-020.2). Administrators create and
/// deactivate accounts, change roles and choose which categories each staff
/// member handles. Anyone else sees a refusal and no controls, and every handler
/// is authorised again by UserService.
/// </summary>
public class UsersModel(
    UserService service,
    IUserRepository users,
    ICategoryRepository categories,
    IAuthorisationPolicy policy,
    ICurrentUser currentUser) : PageModel
{
    public bool Allowed { get; private set; }
    public IReadOnlyList<UserRow> Items { get; private set; } = [];
    public IReadOnlyList<Category> Categories { get; private set; } = [];
    public PageMessage? Message { get; private set; }

    public Guid CurrentUserId => currentUser.Id;

    public static string RoleText(UserRole role) => role switch
    {
        UserRole.ServiceStaff => "Service staff",
        _ => role.ToString()
    };

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostCreateAsync(string? name, UserRole role, CancellationToken ct)
        => await AfterAsync(await service.CreateAsync(currentUser.Id, name, role, ct), "User created.", ct);

    public async Task<IActionResult> OnPostRoleAsync(Guid id, UserRole role, CancellationToken ct)
        => await AfterAsync(await service.ChangeRoleAsync(currentUser.Id, id, role, ct), "Role changed. It applies from the user's next action.", ct);

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken ct)
        => await AfterAsync(await service.DeactivateAsync(currentUser.Id, id, ct), "User deactivated. They can no longer sign in or act.", ct);

    public async Task<IActionResult> OnPostReactivateAsync(Guid id, CancellationToken ct)
        => await AfterAsync(await service.ReactivateAsync(currentUser.Id, id, ct), "User reactivated.", ct);

    public async Task<IActionResult> OnPostScopesAsync(Guid id, List<Guid>? categoryIds, CancellationToken ct)
        => await AfterAsync(await service.SetScopesAsync(currentUser.Id, id, categoryIds ?? [], ct), "Staff categories saved.", ct);

    private async Task<IActionResult> AfterAsync(UserOutcome outcome, string done, CancellationToken ct)
    {
        Message = PageMessage.From(outcome == UserOutcome.Done, outcome switch
        {
            UserOutcome.Done => done,
            UserOutcome.Invalid => $"A user needs a name of at most {AppUser.DisplayNameMax} characters and a known role.",
            UserOutcome.NotFound => "That user no longer exists.",
            UserOutcome.OwnAccount => "You cannot change your own role or deactivate yourself.",
            UserOutcome.NotStaff => "Only service staff have categories.",
            _ => "Only administrators can manage users and roles."
        });

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Allowed = await policy.MayAdministerAsync(currentUser.Id, ct);
        if (Allowed)
        {
            Items = await users.ListAsync(ct);
            Categories = await categories.ListAllAsync(ct);
        }
    }
}
