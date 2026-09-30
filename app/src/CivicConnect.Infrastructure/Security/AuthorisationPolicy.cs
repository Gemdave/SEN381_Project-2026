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
            .Select(r => new { r.RequesterId, r.AssignedToUserId })
            .FirstOrDefaultAsync(ct);

        if (request is null)
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
            RequestAction.Close =>
                user.Role == UserRole.ServiceStaff && request.AssignedToUserId == userId,

            // Anything new is refused until a rule is written for it.
            _ => false
        };
    }
}
