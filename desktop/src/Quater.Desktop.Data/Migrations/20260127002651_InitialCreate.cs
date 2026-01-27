using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Desktop.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Parameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WhoThreshold = table.Column<double>(type: "REAL", nullable: true),
                    MoroccanThreshold = table.Column<double>(type: "REAL", nullable: true),
                    MinValue = table.Column<double>(type: "REAL", nullable: true),
                    MaxValue = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Samples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LocationLatitude = table.Column<double>(type: "REAL", nullable: false),
                    LocationLongitude = table.Column<double>(type: "REAL", nullable: false),
                    LocationDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LocationHierarchy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CollectionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CollectorName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsSynced = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LabId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Samples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastSyncTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RecordsSynced = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ConflictsDetected = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ConflictsResolved = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SampleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParameterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TechnicianName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TestMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ComplianceStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsSynced = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResults_Samples_SampleId",
                        column: x => x.SampleId,
                        principalTable: "Samples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_IsActive",
                table: "Parameters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_Name",
                table: "Parameters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Samples_CollectionDate",
                table: "Samples",
                column: "CollectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_IsSynced",
                table: "Samples",
                column: "IsSynced");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_IsSynced_LastModified",
                table: "Samples",
                columns: new[] { "IsSynced", "LastModified" });

            migrationBuilder.CreateIndex(
                name: "IX_Samples_LabId",
                table: "Samples",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_LastModified",
                table: "Samples",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_Status",
                table: "Samples",
                column: "Status");

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

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_ComplianceStatus",
                table: "TestResults",
                column: "ComplianceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_IsSynced",
                table: "TestResults",
                column: "IsSynced");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_IsSynced_LastModified",
                table: "TestResults",
                columns: new[] { "IsSynced", "LastModified" });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_LastModified",
                table: "TestResults",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_SampleId",
                table: "TestResults",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestDate",
                table: "TestResults",
                column: "TestDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Parameters");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "Samples");
        }
    }
}
