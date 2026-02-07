using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class IntroduceUserLabTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserLabs",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLabs", x => new { x.UserId, x.LabId });
                    table.ForeignKey(
                        name: "FK_UserLabs_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLabs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserLabs_LabId",
                table: "UserLabs",
                column: "LabId");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Labs_LabId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LabId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LabId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            // Enable Row Level Security
            migrationBuilder.Sql("ALTER TABLE \"Samples\" ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE \"TestResults\" ENABLE ROW LEVEL SECURITY;");

            // Create RLS policy for Samples
            migrationBuilder.Sql(@"
    CREATE POLICY lab_isolation_policy ON ""Samples""
    USING (
        current_setting('app.is_system_admin', true) = 'true' 
        OR 
        ""LabId"" = NULLIF(current_setting('app.current_lab_id', true), '')::uuid
    );
");

            // Create RLS policy for TestResults (inherits from Samples)
            migrationBuilder.Sql(@"
    CREATE POLICY lab_isolation_policy ON ""TestResults""
    USING (
        ""SampleId"" IN (SELECT ""Id"" FROM ""Samples"")
    );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop RLS policies
            migrationBuilder.Sql("DROP POLICY IF EXISTS lab_isolation_policy ON \"TestResults\";");
            migrationBuilder.Sql("DROP POLICY IF EXISTS lab_isolation_policy ON \"Samples\";");

            // Disable Row Level Security
            migrationBuilder.Sql("ALTER TABLE \"TestResults\" DISABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE \"Samples\" DISABLE ROW LEVEL SECURITY;");

            migrationBuilder.DropTable(
                name: "UserLabs");

            migrationBuilder.AddColumn<Guid>(
                name: "LabId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LabId",
                table: "Users",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Labs_LabId",
                table: "Users",
                column: "LabId",
                principalTable: "Labs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
