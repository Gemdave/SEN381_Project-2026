using CivicConnect.Application.Abstractions;
using CivicConnect.Application.Access;
using CivicConnect.Domain.Access;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Seed;

/// <summary>The first administrator, taken from configuration and never from the repository (RSK-005).</summary>
public sealed record BootstrapOptions(string? AdminEmail, string? AdminName, string? AdminPassword);

/// <summary>What the bootstrap did, so startup can log it. It never contains a secret.</summary>
public sealed record BootstrapResult(int CategoriesAdded, bool AdministratorCreated, string? Warning);

/// <summary>
/// CR-003 / ADR-007. A fresh deployed database has no categories and nobody who can
/// sign in. This adds the controlled category list and, only while no active
/// administrator exists, one administrator whose temporary password must be changed
/// at first sign-in. It never touches existing data, so running it again is safe.
/// The development seed (sample requests, stand-in users) is never used here.
/// </summary>
public static class ProductionBootstrap
{
    // FR-002. The same controlled list as the development seed.
    public static readonly string[] DefaultCategories =
        ["Fault", "Equipment", "Security", "IT", "Maintenance", "Lost Property", "Other"];

    public static async Task<BootstrapResult> RunAsync(
        CivicConnectDbContext db, IPasswordHasher hasher, BootstrapOptions options, CancellationToken ct = default)
    {
        var categoriesAdded = 0;
        if (!await db.Categories.AnyAsync(ct))
        {
            db.Categories.AddRange(DefaultCategories.Select(Category.Create));
            categoriesAdded = DefaultCategories.Length;
        }

        var administratorCreated = false;
        string? warning = null;

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Administrator && u.IsActive, ct))
        {
            if (!AppUser.IsPlausibleEmail(options.AdminEmail) || string.IsNullOrEmpty(options.AdminPassword))
            {
                warning = "No active administrator exists and Bootstrap:AdminEmail / Bootstrap:AdminPassword are not set, so nobody can administer the system.";
            }
            else if (PasswordPolicy.Check(options.AdminPassword, options.AdminEmail) is { } reason)
            {
                warning = "Bootstrap:AdminPassword was refused: " + reason;
            }
            else
            {
                var email = AppUser.NormaliseEmail(options.AdminEmail);
                var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
                if (existing is null)
                {
                    var name = string.IsNullOrWhiteSpace(options.AdminName) ? "Administrator" : options.AdminName.Trim();
                    var admin = AppUser.Create(name.Length > AppUser.DisplayNameMax ? name[..AppUser.DisplayNameMax] : name, UserRole.Administrator);
                    admin.SetCredentials(email, hasher.Hash(options.AdminPassword), mustChangePassword: true);
                    db.Users.Add(admin);
                    administratorCreated = true;
                }
                else
                {
                    warning = "The bootstrap email already belongs to a user who is not an active administrator. Nothing was changed.";
                }
            }
        }

        await db.SaveChangesAsync(ct);
        return new BootstrapResult(categoriesAdded, administratorCreated, warning);
    }
}
