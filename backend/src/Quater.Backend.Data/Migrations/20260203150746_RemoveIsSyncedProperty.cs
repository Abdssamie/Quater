using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsSyncedProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TestResults_IsSynced",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_IsSynced_UpdatedAt",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_Samples_IsSynced",
                table: "Samples");

            migrationBuilder.DropIndex(
                name: "IX_Samples_IsSynced_UpdatedAt",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Samples");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "TestResults",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Samples",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_IsSynced",
                table: "TestResults",
                column: "IsSynced");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_IsSynced_UpdatedAt",
                table: "TestResults",
                columns: new[] { "IsSynced", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Samples_IsSynced",
                table: "Samples",
                column: "IsSynced");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_IsSynced_UpdatedAt",
                table: "Samples",
                columns: new[] { "IsSynced", "UpdatedAt" });
        }
    }
}
