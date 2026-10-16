using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Requests;
using CivicConnect.Application.Results;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// FR-001 and FR-002. The category list is read from reference data each time,
/// so a new category needs no code change (AC-021.1). Every failing field gets
/// its own message (AC-001.2), and nothing is stored until all of them pass.
/// </summary>
public class SubmitModel(SubmissionService submissions, IRequestQueries queries, ICurrentUser currentUser) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<CategoryOption> Categories { get; private set; } = [];

    // Nullable so the binder reports nothing itself: the service owns validation.
    public class InputModel
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public Guid CategoryId { get; set; }
    }

    public async Task OnGetAsync(CancellationToken ct)
        => Categories = await queries.ListActiveCategoriesAsync(ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var result = await submissions.SubmitAsync(
            currentUser.Id,
            new NewRequest(Input.Title, Input.Description, Input.Location, Input.CategoryId),
            ct);

        if (result.Outcome == SubmissionOutcome.Created)
        {
            return RedirectToPage("/Requests/Details", new { id = result.RequestId, submitted = true });
        }

        ModelState.Clear();
        if (result.Outcome == SubmissionOutcome.Forbidden)
        {
            ModelState.AddModelError(string.Empty, "Only requesters can submit requests.");
        }

        foreach (var (field, message) in result.Errors)
        {
            ModelState.AddModelError(FieldKey(field), message);
        }

        Categories = await queries.ListActiveCategoriesAsync(ct);
        return Page();
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
