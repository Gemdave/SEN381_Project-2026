using CivicConnect.Domain.Access;
using CivicConnect.Domain.ReferenceData;
using CivicConnect.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence;

public class CivicConnectDbContext(DbContextOptions<CivicConnectDbContext> options) : DbContext(options)
{
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestHistoryEntry> RequestHistory => Set<RequestHistoryEntry>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Request>(e =>
        {
            e.ToTable("requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Reference).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Reference).IsUnique();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // FR-004 and NFR-003: every requester query filters on this column.
            e.HasIndex(x => x.RequesterId);

            // FR-008 and NFR-004: the staff queue filters and sorts on these.
            e.HasIndex(x => new { x.Status, x.CategoryId, x.AssignedToUserId });
        });

        b.Entity<RequestHistoryEntry>(e =>
        {
            e.ToTable("request_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.RequestId);

            // NFR-007: nothing in the application updates or deletes these rows.
            // RSK-013 proposes tightening this with insert and select only grants
            // on the application's database role.
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(60).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<AppUser>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        });
    }
}
