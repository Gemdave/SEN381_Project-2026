using CivicConnect.Domain.Access;
using CivicConnect.Domain.Audit;
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
    public DbSet<StaffCategoryScope> StaffCategoryScopes => Set<StaffCategoryScope>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<RequestAttachment> Attachments => Set<RequestAttachment>();

    /// <summary>FR-001. Numbers for request references (REQ-001000 and up).</summary>
    public const string ReferenceSequence = "request_reference_seq";

    public DbSet<RequesterFeedback> Feedback => Set<RequesterFeedback>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasSequence<long>(ReferenceSequence).StartsAt(1000);

        b.Entity<Request>(e =>
        {
            e.ToTable("requests", t =>
            {
                // The database refuses states the application should never produce (ASR-01, NFR-005).
                t.HasCheckConstraint(
                    "ck_requests_status",
                    "\"Status\" IN ('Received', 'Assigned', 'InProgress', 'Resolved', 'Closed', 'Rejected')");

                // CR-009: a rejected request was never accepted, so like a received one it has no owner.
                t.HasCheckConstraint(
                    "ck_requests_owner_matches_status",
                    "(\"Status\" IN ('Received', 'Rejected') AND \"AssignedToUserId\" IS NULL) OR (\"Status\" NOT IN ('Received', 'Rejected') AND \"AssignedToUserId\" IS NOT NULL)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Reference).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Reference).IsUnique();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).IsRequired();
            e.Property(x => x.Location).HasMaxLength(Request.LocationMax).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            // FR-004 and NFR-003: every requester query filters on this column.
            e.HasIndex(x => x.RequesterId);

            // FR-008 and NFR-004: the staff queue filters and sorts on these.
            e.HasIndex(x => new { x.Status, x.CategoryId, x.AssignedToUserId });

            // FR-016: the overdue count reads open requests by due time.
            e.HasIndex(x => new { x.Status, x.TargetResponseAtUtc });
            e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<RequestHistoryEntry>(e =>
        {
            e.ToTable("request_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.RequestId);
            e.HasOne<Request>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);

            // NFR-007: nothing in the application updates or deletes these rows.
            // RSK-013 proposes tightening this with insert and select only grants
            // on the application's database role.
        });

        b.Entity<Category>(e =>
        {
            // FR-016 (CR-005): the database refuses a target outside the allowed range.
            e.ToTable("categories", t => t.HasCheckConstraint(
                "ck_categories_target_hours",
                $"\"TargetResponseHours\" BETWEEN {ResponseTarget.MinHours} AND {ResponseTarget.MaxHours}"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(60).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<RequesterFeedback>(e =>
        {
            e.ToTable("requester_feedback");
            e.HasKey(x => x.Id);
            e.Property(x => x.Message).IsRequired();
            e.HasIndex(x => x.RequestId);
            e.HasIndex(x => x.RequesterId);
            e.HasOne<Request>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AppUser>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(AppUser.DisplayNameMax).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(AppUser.EmailMax);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(AppUser.PasswordHashMax);
        });

        b.Entity<RequestAttachment>(e =>
        {
            // FR-006: stored with the request, so one backup holds everything (ADR-007).
            e.ToTable("request_attachments", t => t.HasCheckConstraint(
                "ck_request_attachments_size",
                $"\"SizeBytes\" BETWEEN 1 AND {RequestAttachment.MaxBytes}"));
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(RequestAttachment.FileNameMax).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Content).IsRequired();
            e.HasIndex(x => x.RequestId);
            e.HasOne<Request>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<StaffCategoryScope>(e =>
        {
            // FR-008: the queue and the policy both look up (user, category).
            e.ToTable("staff_category_scopes");
            e.HasKey(x => new { x.UserId, x.CategoryId });
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AuditEntry>(e =>
        {
            // FR-022, NFR-007: insert only, enforced by a trigger in the migration.
            e.ToTable("audit_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(40).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.OccurredAtUtc);
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
