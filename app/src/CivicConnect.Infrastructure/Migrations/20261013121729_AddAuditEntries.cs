using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "text", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_entries_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_ActorUserId",
                table: "audit_entries",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_OccurredAtUtc",
                table: "audit_entries",
                column: "OccurredAtUtc");

            // FR-022, NFR-007: like request_history, audit entries are insert-only.
            migrationBuilder.Sql(@"
CREATE FUNCTION audit_entries_block_changes() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'audit_entries is insert-only';
END;
$$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
CREATE TRIGGER audit_entries_insert_only
BEFORE UPDATE OR DELETE ON audit_entries
FOR EACH ROW EXECUTE FUNCTION audit_entries_block_changes();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER audit_entries_insert_only ON audit_entries;");
            migrationBuilder.Sql("DROP FUNCTION audit_entries_block_changes();");

            migrationBuilder.DropTable(
                name: "audit_entries");
        }
    }
}
