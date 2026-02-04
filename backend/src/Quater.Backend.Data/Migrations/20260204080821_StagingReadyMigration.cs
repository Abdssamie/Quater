using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class StagingReadyMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TestResult_Measurement_ParameterId",
                table: "TestResults",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestResult_Measurement_ParameterId",
                table: "TestResults",
                column: "TestResult_Measurement_ParameterId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Parameters_TestResult_Measurement_ParameterId",
                table: "TestResults",
                column: "TestResult_Measurement_ParameterId",
                principalTable: "Parameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Parameters_TestResult_Measurement_ParameterId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_TestResult_Measurement_ParameterId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "TestResult_Measurement_ParameterId",
                table: "TestResults");
        }
    }
}
