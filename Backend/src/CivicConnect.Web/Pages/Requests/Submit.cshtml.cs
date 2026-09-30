using CivicConnect.Core.Models;
using CivicConnect.Core.Services;
using CivicConnect.Web.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// FR-001 and FR-002. The category list is read from reference data every time,
/// so an administrator adding one needs no code change (AC-021.1). Validation
/// failures are reported per field, which is what AC-001.2 asks for.
/// </summary>
public class SubmitModel(RequestService requests, CategoryService categories, PageUserAccessor pageUser) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Category> Categories { get; private set; } = [];

    // A plain class with settable properties: the model binder writes straight
    // into these, which init only properties on a record do not reliably allow.
    public class InputModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public short CategoryId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var user = await pageUser.TryGetAsync(HttpContext, ct);
        if (user is null)
        {
            return RedirectToPage("/Index");
        }

        Categories = await categories.ListActiveAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var user = await pageUser.TryGetAsync(HttpContext, ct);
        if (user is null)
        {
            return RedirectToPage("/Index");
        }

        Categories = await categories.ListActiveAsync(ct);

        try
        {
            var created = await requests.SubmitAsync(
                user,
                new NewRequest(Input.Title, Input.Description, Input.Location, Input.CategoryId),
                ct);

            return RedirectToPage("/Requests/Details", new { id = created.Id, submitted = true });
        }
        catch (ValidationFailedException failed)
        {
            // One message against each field that needs attention (AC-001.2).
            foreach (var (field, messages) in failed.Errors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(FieldKey(field), message);
                }
            }

            return Page();
        }
        catch (ForbiddenException denied)
        {
            ModelState.AddModelError(string.Empty, denied.Message);
            return Page();
        }
    }

    private static string FieldKey(string field) => field switch
    {
        "title" => "Input.Title",
        "description" => "Input.Description",
        "location" => "Input.Location",
        "categoryId" => "Input.CategoryId",
        _ => string.Empty
    };
}
