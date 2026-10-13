using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoryTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE FUNCTION request_history_block_changes() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'request_history is insert-only';
END;
$$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
CREATE TRIGGER request_history_insert_only
BEFORE UPDATE OR DELETE ON request_history
FOR EACH ROW EXECUTE FUNCTION request_history_block_changes();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER request_history_insert_only ON request_history;");
            migrationBuilder.Sql("DROP FUNCTION request_history_block_changes();");
        }
    }
}
