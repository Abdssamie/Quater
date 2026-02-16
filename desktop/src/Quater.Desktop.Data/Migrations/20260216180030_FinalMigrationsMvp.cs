using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Desktop.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalMigrationsMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_User_UserId",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogArchive_User_UserId",
                table: "AuditLogArchive");

            migrationBuilder.DropForeignKey(
                name: "FK_Samples_Lab_LabId",
                table: "Samples");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Lab_LabId",
                table: "User");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_IsSynced_LastModified",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_LastModified",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_Samples_IsSynced_LastModified",
                table: "Samples");

            migrationBuilder.DropIndex(
                name: "IX_Samples_LastModified",
                table: "Samples");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_LabId",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Lab",
                table: "Lab");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogArchive",
                table: "AuditLogArchive");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLog",
                table: "AuditLog");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "ParameterName",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "SyncVersion",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "SyncVersion",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "User");

            migrationBuilder.DropColumn(
                name: "LabId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "User");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Lab");

            migrationBuilder.DropColumn(
                name: "ConflictResolutionNotes",
                table: "AuditLogArchive");

            migrationBuilder.DropColumn(
                name: "ConflictResolutionNotes",
                table: "AuditLog");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Lab",
                newName: "Labs");

            migrationBuilder.RenameTable(
                name: "AuditLogArchive",
                newName: "AuditLogArchives");

            migrationBuilder.RenameTable(
                name: "AuditLog",
                newName: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "SyncVersion",
                table: "TestResults",
                newName: "VoidedTestResultId");

            migrationBuilder.RenameColumn(
                name: "LastModified",
                table: "TestResults",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "TestResults",
                newName: "ParameterId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogArchive_UserId",
                table: "AuditLogArchives",
                newName: "IX_AuditLogArchives_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLogs",
                newName: "IX_AuditLogs_UserId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSyncedAt",
                table: "TestResults",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "TestResults",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacedByTestResultId",
                table: "TestResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TestResult_ParameterId",
                table: "TestResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "TestResults",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Samples",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "Parameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Parameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "TwoFactorEnabled",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "LockoutEnabled",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "AccessFailedCount",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "Labs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Labs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTruncated",
                table: "AuditLogArchives",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTruncated",
                table: "AuditLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Labs",
                table: "Labs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogArchives",
                table: "AuditLogArchives",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "UserLabs",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LabId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "IX_TestResults_IsSynced_UpdatedAt",
                table: "TestResults",
                columns: new[] { "IsSynced", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestResult_ParameterId",
                table: "TestResults",
                column: "TestResult_ParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_IsSynced_UpdatedAt",
                table: "Samples",
                columns: new[] { "IsSynced", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_IsSynced",
                table: "Parameters",
                column: "IsSynced");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedUserName",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Labs_IsActive",
                table: "Labs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_Name",
                table: "Labs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UserLabs_LabId",
                table: "UserLabs",
                column: "LabId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogArchives_Users_UserId",
                table: "AuditLogArchives",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Samples_Labs_LabId",
                table: "Samples",
                column: "LabId",
                principalTable: "Labs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_Parameters_TestResult_ParameterId",
                table: "TestResults",
                column: "TestResult_ParameterId",
                principalTable: "Parameters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogArchives_Users_UserId",
                table: "AuditLogArchives");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Samples_Labs_LabId",
                table: "Samples");

            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_Parameters_TestResult_ParameterId",
                table: "TestResults");

            migrationBuilder.DropTable(
                name: "UserLabs");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_IsSynced_UpdatedAt",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_TestResult_ParameterId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_Samples_IsSynced_UpdatedAt",
                table: "Samples");

            migrationBuilder.DropIndex(
                name: "IX_Parameters_IsSynced",
                table: "Parameters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedUserName",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Labs",
                table: "Labs");

            migrationBuilder.DropIndex(
                name: "IX_Labs_IsActive",
                table: "Labs");

            migrationBuilder.DropIndex(
                name: "IX_Labs_Name",
                table: "Labs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogArchives",
                table: "AuditLogArchives");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "ReplacedByTestResultId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "TestResult_ParameterId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "IsTruncated",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IsTruncated",
                table: "AuditLogArchives");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "Labs",
                newName: "Lab");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLog");

            migrationBuilder.RenameTable(
                name: "AuditLogArchives",
                newName: "AuditLogArchive");

            migrationBuilder.RenameColumn(
                name: "VoidedTestResultId",
                table: "TestResults",
                newName: "SyncVersion");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "TestResults",
                newName: "LastModified");

            migrationBuilder.RenameColumn(
                name: "ParameterId",
                table: "TestResults",
                newName: "CreatedDate");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLog",
                newName: "IX_AuditLog_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogArchives_UserId",
                table: "AuditLogArchive",
                newName: "IX_AuditLogArchive_UserId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSyncedAt",
                table: "TestResults",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "TestResults",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ParameterName",
                table: "TestResults",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TestResults",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Samples",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Samples",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Samples",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Samples",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "Samples",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Samples",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "Parameters",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Parameters",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "Parameters",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Parameters",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "Parameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "TwoFactorEnabled",
                table: "User",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "User",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "LockoutEnabled",
                table: "User",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "EmailConfirmed",
                table: "User",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "AccessFailedCount",
                table: "User",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "User",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "User",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "User",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "LabId",
                table: "User",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "User",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "User",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "User",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "Lab",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Lab",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ConflictResolutionNotes",
                table: "AuditLog",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConflictResolutionNotes",
                table: "AuditLogArchive",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lab",
                table: "Lab",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLog",
                table: "AuditLog",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogArchive",
                table: "AuditLogArchive",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ConflictsDetected = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ConflictsResolved = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LastSyncTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordsSynced = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SyncVersion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncLogs_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_IsSynced_LastModified",
                table: "TestResults",
                columns: new[] { "IsSynced", "LastModified" });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_LastModified",
                table: "TestResults",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_IsSynced_LastModified",
                table: "Samples",
                columns: new[] { "IsSynced", "LastModified" });

            migrationBuilder.CreateIndex(
                name: "IX_Samples_LastModified",
                table: "Samples",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_User_LabId",
                table: "User",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_DeviceId",
                table: "SyncLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_LastSyncTimestamp",
                table: "SyncLogs",
                column: "LastSyncTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_UserId",
                table: "SyncLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_User_UserId",
                table: "AuditLog",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogArchive_User_UserId",
                table: "AuditLogArchive",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Samples_Lab_LabId",
                table: "Samples",
                column: "LabId",
                principalTable: "Lab",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Lab_LabId",
                table: "User",
                column: "LabId",
                principalTable: "Lab",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
