using CivicConnect.Application.Abstractions;
using CivicConnect.Domain.Access;
using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Security;

/// <summary>
/// ADR-005. One decision point, refusing by default. Role rules and ownership
/// rules are answered here together, because ownership cannot be decided from
/// the role alone (NFR-003, AC-008.2).
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

        // FR-008: staff act only on requests in their scoped categories, so the
        // rules below never see an out-of-scope request (AC-008.1, AC-008.2).
        var inScope = user.Role == UserRole.ServiceStaff
            && await db.StaffCategoryScopes.AnyAsync(
                s => s.UserId == userId && s.CategoryId == request.CategoryId, ct);
        if (!RequestAccess.WithinScope(user.Role, inScope))
        {
            return false;
        }

        return action switch
        {
            RequestAction.View => user.Role switch
            {
                UserRole.Requester => request.RequesterId == userId,
                UserRole.ServiceStaff => true,
                UserRole.Management => true,
                UserRole.Administrator => true,
                _ => false
            },
            RequestAction.Assign => user.Role == UserRole.ServiceStaff,
            RequestAction.Transition or RequestAction.AddNote =>
                user.Role == UserRole.ServiceStaff && request.AssignedToUserId == userId,
            // Closing (AC-014.2): the staff member who owns it, or an administrator.
            RequestAction.Close =>
                (user.Role == UserRole.ServiceStaff && request.AssignedToUserId == userId)
                || user.Role == UserRole.Administrator,

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

    public async Task<bool> MayViewOversightAsync(Guid userId, CancellationToken ct)
        => await RoleOfAsync(userId, ct) is { } role && RequestAccess.MayViewOversight(role);

    public async Task<bool> MayAdministerAsync(Guid userId, CancellationToken ct)
        => await RoleOfAsync(userId, ct) is { } role && RequestAccess.MayAdminister(role);

    private Task<UserRole?> RoleOfAsync(Guid userId, CancellationToken ct)
        => db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(ct);
}
