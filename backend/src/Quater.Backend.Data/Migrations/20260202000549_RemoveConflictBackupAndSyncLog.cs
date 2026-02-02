using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConflictBackupAndSyncLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogArchive_ConflictBackups_ConflictBackupId",
                table: "AuditLogArchive");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_ConflictBackups_ConflictBackupId",
                table: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ConflictBackups");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ConflictBackupId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogArchive_ConflictBackupId",
                table: "AuditLogArchive");

            migrationBuilder.DropColumn(
                name: "ConflictBackupId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ConflictResolutionNotes",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ConflictBackupId",
                table: "AuditLogArchive");

            migrationBuilder.DropColumn(
                name: "ConflictResolutionNotes",
                table: "AuditLogArchive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConflictBackupId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConflictResolutionNotes",
                table: "AuditLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConflictBackupId",
                table: "AuditLogArchive",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConflictResolutionNotes",
                table: "AuditLogArchive",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConflictBackups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientVersion = table.Column<string>(type: "text", nullable: false),
                    ConflictDetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<int>(type: "integer", maxLength: 100, nullable: false),
                    ResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolutionStrategy = table.Column<string>(type: "text", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ServerVersion = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConflictsDetected = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ConflictsResolved = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastSyncTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordsSynced = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    SyncVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ConflictBackupId",
                table: "AuditLogs",
                column: "ConflictBackupId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogArchive_ConflictBackupId",
                table: "AuditLogArchive",
                column: "ConflictBackupId");

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

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_DeviceId",
                table: "SyncLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_DeviceId_LastSyncTimestamp",
                table: "SyncLogs",
                columns: new[] { "DeviceId", "LastSyncTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_LastSyncTimestamp",
                table: "SyncLogs",
                column: "LastSyncTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_UserId",
                table: "SyncLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogArchive_ConflictBackups_ConflictBackupId",
                table: "AuditLogArchive",
                column: "ConflictBackupId",
                principalTable: "ConflictBackups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_ConflictBackups_ConflictBackupId",
                table: "AuditLogs",
                column: "ConflictBackupId",
                principalTable: "ConflictBackups",
                principalColumn: "Id");
        }
    }
}
