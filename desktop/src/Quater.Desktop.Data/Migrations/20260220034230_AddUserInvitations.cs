using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Desktop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoroccanThreshold",
                table: "Parameters");

            migrationBuilder.RenameColumn(
                name: "WhoThreshold",
                table: "Parameters",
                newName: "Threshold");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Threshold",
                table: "Parameters",
                newName: "WhoThreshold");

            migrationBuilder.AddColumn<double>(
                name: "MoroccanThreshold",
                table: "Parameters",
                type: "REAL",
                nullable: true);
        }
    }
}
