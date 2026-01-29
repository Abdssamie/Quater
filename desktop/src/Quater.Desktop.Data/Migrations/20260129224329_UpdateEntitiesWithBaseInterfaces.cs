using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Desktop.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesWithBaseInterfaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "User",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

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

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TestResults",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TestResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TestResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "TestResults",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TestResults",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "TestResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TestResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TestResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "SyncLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "SyncLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Samples",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Samples",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Samples",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Samples",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Samples",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "Samples",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Samples",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Samples",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Parameters",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Parameters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Parameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Parameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Parameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Parameters",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Parameters",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SyncVersion",
                table: "Parameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Parameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Parameters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Lab",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Lab",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Lab",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Lab",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Lab",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Lab",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Lab",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Lab",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "User");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "User");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "User");

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
                table: "Lab");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Lab");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Lab");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Lab");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Lab");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Lab");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Lab");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Lab");
        }
    }
}
