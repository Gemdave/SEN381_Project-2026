using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_requests_owner_matches_status",
                table: "requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_requests_status",
                table: "requests");

            // FR-019: every existing account stays active.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TargetResponseAtUtc",
                table: "requests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // FR-016 (CR-005): existing categories start on the default target of 72 hours.
            migrationBuilder.AddColumn<int>(
                name: "TargetResponseHours",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 72);

            // Existing requests are given the due time they would have had at submission.
            migrationBuilder.Sql(
                "UPDATE requests SET \"TargetResponseAtUtc\" = \"CreatedAtUtc\" + interval '72 hours';");

            migrationBuilder.CreateTable(
                name: "request_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_attachments", x => x.Id);
                    table.CheckConstraint("ck_request_attachments_size", "\"SizeBytes\" BETWEEN 1 AND 5242880");
                    table.ForeignKey(
                        name: "FK_request_attachments_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_attachments_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_requests_Status_TargetResponseAtUtc",
                table: "requests",
                columns: new[] { "Status", "TargetResponseAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_requests_owner_matches_status",
                table: "requests",
                sql: "(\"Status\" IN ('Received', 'Rejected') AND \"AssignedToUserId\" IS NULL) OR (\"Status\" NOT IN ('Received', 'Rejected') AND \"AssignedToUserId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_requests_status",
                table: "requests",
                sql: "\"Status\" IN ('Received', 'Assigned', 'InProgress', 'Resolved', 'Closed', 'Rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_categories_target_hours",
                table: "categories",
                sql: "\"TargetResponseHours\" BETWEEN 1 AND 2160");

            migrationBuilder.CreateIndex(
                name: "IX_request_attachments_RequestId",
                table: "request_attachments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_request_attachments_UploadedByUserId",
                table: "request_attachments",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_attachments");

            migrationBuilder.DropIndex(
                name: "IX_requests_Status_TargetResponseAtUtc",
                table: "requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_requests_owner_matches_status",
                table: "requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_requests_status",
                table: "requests");

            migrationBuilder.DropCheckConstraint(
                name: "ck_categories_target_hours",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TargetResponseAtUtc",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "TargetResponseHours",
                table: "categories");

            migrationBuilder.AddCheckConstraint(
                name: "ck_requests_owner_matches_status",
                table: "requests",
                sql: "(\"Status\" = 'Received' AND \"AssignedToUserId\" IS NULL) OR (\"Status\" <> 'Received' AND \"AssignedToUserId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_requests_status",
                table: "requests",
                sql: "\"Status\" IN ('Received', 'Assigned', 'InProgress', 'Resolved', 'Closed')");
        }
    }
}
