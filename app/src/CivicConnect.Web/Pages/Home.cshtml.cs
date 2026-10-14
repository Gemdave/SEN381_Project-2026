using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.Requests;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages;

/// <summary>
/// The landing page. It says who is signed in and offers only the actions their
/// role allows, decided by the same policy checks as the navigation (ADR-004);
/// every page it links to checks again. A deactivated or unknown user is offered
/// nothing (AC-019.1).
/// </summary>
public class HomeModel(
    IUserRepository users,
    IRequestQueries requests,
    IQueueRepository queue,
    IOversightQueries oversight,
    IAuthorisationPolicy policy,
    ICurrentUser currentUser) : PageModel
{
    /// <summary>The queue is paged, so the waiting count stops here and shows as "100+".</summary>
    public const int QueueCountLimit = 100;

    public string? Name { get; private set; }
    public UserRole Role { get; private set; }
    public bool SignedIn => Name is not null;

    public bool CanSubmit { get; private set; }
    public bool IsStaff { get; private set; }
    public bool CanOversee { get; private set; }
    public bool CanSeeSummary { get; private set; }
    public bool CanAdminister { get; private set; }

    public int OpenRequests { get; private set; }
    public int WaitingInQueue { get; private set; }
    public OversightSummary? Figures { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var user = await users.FindAsync(currentUser.Id, ct);
        if (user is not { IsActive: true })
        {
            return;
        }

        Name = user.DisplayName;
        Role = user.Role;

        CanSubmit = await policy.MaySubmitAsync(user.Id, ct);
        IsStaff = await queue.IsServiceStaffAsync(user.Id, ct);
        CanOversee = await policy.MayViewOversightAsync(user.Id, ct);
        CanSeeSummary = await policy.MayViewSummaryAsync(user.Id, ct);
        CanAdminister = await policy.MayAdministerAsync(user.Id, ct);

        if (CanSubmit)
        {
            // Scoped to this requester inside the query (NFR-003); it holds their 50 most recent.
            var own = await requests.ListForRequesterAsync(user.Id, null, ct);
            OpenRequests = own.Count(r => ResponseTarget.IsOpen(r.Status));
        }

        if (IsStaff)
        {
            // FR-008: only the received requests in this staff member's categories.
            var waiting = await queue.SearchAsync(
                user.Id, new QueueFilter(Status: RequestStatus.Received, PageSize: QueueCountLimit), ct);
            WaitingInQueue = waiting.Count;
        }

        if (CanSeeSummary)
        {
            Figures = await oversight.SummaryAsync(new OversightFilter(), ct);
        }
    }

    public static string Plural(int count, string one, string many) => count == 1 ? one : many;
}
