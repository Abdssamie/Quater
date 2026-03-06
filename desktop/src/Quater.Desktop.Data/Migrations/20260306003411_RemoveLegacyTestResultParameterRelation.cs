using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Desktop.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyTestResultParameterRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Parameters_TestResult_ParameterId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_TestResult_ParameterId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "TestResult_ParameterId",
                table: "TestResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TestResult_ParameterId",
                table: "TestResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestResult_ParameterId",
                table: "TestResults",
                column: "TestResult_ParameterId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Parameters_TestResult_ParameterId",
                table: "TestResults",
                column: "TestResult_ParameterId",
                principalTable: "Parameters",
                principalColumn: "Id");
        }
    }
}
