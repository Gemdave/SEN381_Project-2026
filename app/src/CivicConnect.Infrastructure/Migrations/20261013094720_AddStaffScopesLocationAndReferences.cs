using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffScopesLocationAndReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "request_reference_seq",
                startValue: 1000L);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "staff_category_scopes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_category_scopes", x => new { x.UserId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_staff_category_scopes_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_category_scopes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_staff_category_scopes_CategoryId",
                table: "staff_category_scopes",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_category_scopes");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "requests");

            migrationBuilder.DropSequence(
                name: "request_reference_seq");
        }
    }
}
