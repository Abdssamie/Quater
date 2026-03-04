using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAuditLogArchiveEnumConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"AuditLogArchive\" ALTER COLUMN \"EntityType\" TYPE text USING \"EntityType\"::text;");
            migrationBuilder.Sql("ALTER TABLE \"AuditLogArchive\" ALTER COLUMN \"Action\" TYPE text USING \"Action\"::text;");

            migrationBuilder.Sql(@"
    UPDATE ""AuditLogArchive""
    SET ""EntityType"" = CASE ""EntityType""
        WHEN '1' THEN 'Lab'
        WHEN '2' THEN 'Sample'
        WHEN '3' THEN 'TestResult'
        WHEN '4' THEN 'Parameter'
        WHEN '5' THEN 'AuditLog'
        WHEN '6' THEN 'AuditLogArchive'
        ELSE ""EntityType""
    END;
");

            migrationBuilder.Sql(@"
    UPDATE ""AuditLogArchive""
    SET ""Action"" = CASE ""Action""
        WHEN '0' THEN 'Create'
        WHEN '1' THEN 'Update'
        WHEN '2' THEN 'Delete'
        WHEN '3' THEN 'Restore'
        WHEN '4' THEN 'ConflictResolution'
        ELSE ""Action""
    END;
");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "AuditLogArchive",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditLogArchive",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
    UPDATE ""AuditLogArchive""
    SET ""Action"" = CASE ""Action""
        WHEN 'Create' THEN '0'
        WHEN 'Update' THEN '1'
        WHEN 'Delete' THEN '2'
        WHEN 'Restore' THEN '3'
        WHEN 'ConflictResolution' THEN '4'
        ELSE ""Action""
    END;
");

            migrationBuilder.Sql(@"
    UPDATE ""AuditLogArchive""
    SET ""EntityType"" = CASE ""EntityType""
        WHEN 'Lab' THEN '1'
        WHEN 'Sample' THEN '2'
        WHEN 'TestResult' THEN '3'
        WHEN 'Parameter' THEN '4'
        WHEN 'AuditLog' THEN '5'
        WHEN 'AuditLogArchive' THEN '6'
        ELSE ""EntityType""
    END;
");

            migrationBuilder.Sql("ALTER TABLE \"AuditLogArchive\" ALTER COLUMN \"EntityType\" TYPE integer USING \"EntityType\"::integer;");
            migrationBuilder.Sql("ALTER TABLE \"AuditLogArchive\" ALTER COLUMN \"Action\" TYPE integer USING \"Action\"::integer;");

            migrationBuilder.AlterColumn<int>(
                name: "EntityType",
                table: "AuditLogArchive",
                type: "integer",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "Action",
                table: "AuditLogArchive",
                type: "integer",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
