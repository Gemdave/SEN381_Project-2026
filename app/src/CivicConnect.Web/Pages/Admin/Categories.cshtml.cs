using CivicConnect.Application.Abstractions;
using CivicConnect.Application.ReferenceData;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Admin;

/// <summary>
/// FR-021 (AC-021.1). Administrators add, rename, retire and restore categories;
/// the submit form reads the live list, so no code change is needed. Each
/// handler is authorised again by CategoryService.
/// </summary>
public class CategoriesModel(
    CategoryService service,
    ICategoryRepository categories,
    IAuthorisationPolicy policy,
    ICurrentUser currentUser) : PageModel
{
    public bool Allowed { get; private set; }
    public IReadOnlyList<Category> Items { get; private set; } = [];
    public string? Message { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostAddAsync(string? name, CancellationToken ct)
        => await AfterAsync(await service.AddAsync(currentUser.Id, name, ct), "Category added.", ct);

    public async Task<IActionResult> OnPostRenameAsync(Guid id, string? name, CancellationToken ct)
        => await AfterAsync(await service.RenameAsync(currentUser.Id, id, name, ct), "Category renamed.", ct);

    public async Task<IActionResult> OnPostRetireAsync(Guid id, CancellationToken ct)
        => await AfterAsync(await service.RetireAsync(currentUser.Id, id, ct), "Category retired. It no longer appears on the submit form.", ct);

    public async Task<IActionResult> OnPostRestoreAsync(Guid id, CancellationToken ct)
        => await AfterAsync(await service.RestoreAsync(currentUser.Id, id, ct), "Category restored.", ct);

    private async Task<IActionResult> AfterAsync(CategoryOutcome outcome, string done, CancellationToken ct)
    {
        Message = outcome switch
        {
            CategoryOutcome.Done => done,
            CategoryOutcome.Invalid => $"A category needs a name of at most {CategoryService.NameMax} characters.",
            CategoryOutcome.Duplicate => "A category with that name already exists.",
            CategoryOutcome.NotFound => "That category no longer exists.",
            _ => "Only administrators can change categories."
        };

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Allowed = await policy.MayAdministerAsync(currentUser.Id, ct);
        if (Allowed)
        {
            Items = await categories.ListAllAsync(ct);
        }
    }
}
