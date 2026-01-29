using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesWithBaseInterfaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TestResults",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TestResults",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TestResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "TestResults",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TestResults",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "TestResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TestResults",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TestResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "SyncLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "SyncLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Samples",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Samples",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Samples",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Samples",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Samples",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "Samples",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Samples",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Samples",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Parameters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Parameters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Parameters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Parameters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Parameters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Parameters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Parameters",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "Parameters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Parameters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Parameters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Labs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Labs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Labs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Labs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Labs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Labs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Labs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Labs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConflictBackups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServerVersion = table.Column<string>(type: "text", nullable: false),
                    ClientVersion = table.Column<string>(type: "text", nullable: false),
                    ResolutionStrategy = table.Column<string>(type: "text", nullable: false),
                    ConflictDetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictBackups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConflictBackups_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConflictBackups_ConflictDetectedAt",
                table: "ConflictBackups",
                column: "ConflictDetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictBackups_DeviceId",
                table: "ConflictBackups",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictBackups_EntityType_EntityId",
                table: "ConflictBackups",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConflictBackups_LabId",
                table: "ConflictBackups",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictBackups_ResolvedAt",
                table: "ConflictBackups",
                column: "ResolvedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConflictBackups");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "SyncVersion",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "SyncLogs");

            migrationBuilder.DropColumn(
                name: "SyncVersion",
                table: "SyncLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "SyncVersion",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "SyncVersion",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Labs");
        }
    }
}
