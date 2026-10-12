using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Security;

/// <summary>
/// ADR-005. One decision point, refusing by default. Role rules and ownership
/// rules are answered here together, because ownership cannot be decided from
/// the role alone (NFR-003, AC-008.2). The rules themselves live in
/// RequestAccess; this class only gathers the facts they need.
/// </summary>
public sealed class AuthorisationPolicy(CivicConnectDbContext db) : IAuthorisationPolicy
{
    public async Task<bool> MayAsync(Guid userId, RequestAction action, Guid requestId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return false;
        }

        var request = await db.Requests.AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new { r.RequesterId, r.CategoryId, r.AssignedToUserId })
            .FirstOrDefaultAsync(ct);

        if (request is null)
        {
            return false;
        }

        // FR-008: staff act only on requests in their scoped categories.
        var inScope = user.Role == UserRole.ServiceStaff
            && await db.StaffCategoryScopes.AnyAsync(
                s => s.UserId == userId && s.CategoryId == request.CategoryId, ct);

        return action switch
        {
            RequestAction.View => RequestAccess.MayView(user.Role, userId, request.RequesterId, inScope),
            RequestAction.Assign => RequestAccess.MayAssign(user.Role, inScope),
            RequestAction.Transition or RequestAction.AddNote or RequestAction.Close =>
                RequestAccess.MayWorkOn(user.Role, userId, request.AssignedToUserId, inScope),

            // Anything new is refused until a rule is written for it.
            _ => false
        };
    }

    public async Task<bool> MaySubmitAsync(Guid userId, CancellationToken ct)
    {
        var role = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(ct);

        return role is { } known && RequestAccess.MaySubmit(known);
    }
}
