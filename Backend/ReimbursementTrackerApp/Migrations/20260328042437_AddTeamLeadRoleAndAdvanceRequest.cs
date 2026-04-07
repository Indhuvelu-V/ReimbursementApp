using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReimbursementTrackerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamLeadRoleAndAdvanceRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdvanceRequest",
                table: "Expenses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdvanceRequest",
                table: "Expenses");
        }
    }
}
