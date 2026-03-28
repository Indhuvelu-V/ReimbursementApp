using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReimbursementTrackerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProcessedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessedByName",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedByUserId",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedByName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
