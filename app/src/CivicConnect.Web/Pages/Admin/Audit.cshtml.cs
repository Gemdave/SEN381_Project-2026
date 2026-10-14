using CivicConnect.Application.Abstractions;
using CivicConnect.Web.Models;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Admin;

/// <summary>
/// FR-022 (AC-022.1). Key controlled actions with actor and time, newest first:
/// status changes, assignments, notes and submissions from the request history,
/// and category changes from the audit entries. Role changes join when FR-020
/// is built.
/// </summary>
public class AuditModel(
    IAuditRepository audit,
    IRequestQueries queries,
    IAuthorisationPolicy policy,
    ICurrentUser currentUser) : PageModel
{
    public const int Rows = 100;

    [BindProperty(SupportsGet = true)]
    public AuditKind Kind { get; set; } = AuditKind.All;

    public bool Allowed { get; private set; }
    public IReadOnlyList<AuditItem> Items { get; private set; } = [];
    public HistoryView People { get; private set; } = new([], new Dictionary<Guid, string>());

    public async Task OnGetAsync(CancellationToken ct)
    {
        Allowed = await policy.MayAdministerAsync(currentUser.Id, ct);
        if (!Allowed)
        {
            return;
        }

        Items = await audit.RecentAsync(Kind, Rows, ct);
        var ids = Items.Select(i => i.ActorUserId)
            .Concat(Items.Select(i => Guid.TryParse(i.NewValue, out var owner) ? owner : Guid.Empty))
            .Where(id => id != Guid.Empty);
        People = new HistoryView([], await queries.DisplayNamesAsync(ids, ct));
    }

    public static string Label(string action) => action switch
    {
        "CategoryAdded" => "Category added",
        "CategoryRenamed" => "Category renamed",
        "CategoryRetired" => "Category retired",
        "CategoryRestored" => "Category restored",
        _ => HistoryView.ActionText(action)
    };
}
