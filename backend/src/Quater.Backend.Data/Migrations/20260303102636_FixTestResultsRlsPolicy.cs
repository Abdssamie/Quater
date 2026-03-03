using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quater.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTestResultsRlsPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the broken policy that was missing the lab-ID filter
            migrationBuilder.Sql(@"DROP POLICY IF EXISTS lab_isolation_policy ON ""TestResults"";");

            // Re-create the policy with the correct USING clause that applies the lab-ID filter
            migrationBuilder.Sql(@"
    CREATE POLICY lab_isolation_policy ON ""TestResults""
    USING (
        current_setting('app.is_system_admin', true) = 'true'
        OR EXISTS (
            SELECT 1 FROM ""Samples""
            WHERE ""Samples"".""Id"" = ""TestResults"".""SampleId""
              AND ""Samples"".""LabId"" = NULLIF(current_setting('app.current_lab_id', true), '')::uuid
        )
    );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the original (broken) policy for rollback safety
            migrationBuilder.Sql(@"DROP POLICY IF EXISTS lab_isolation_policy ON ""TestResults"";");

            migrationBuilder.Sql(@"
    CREATE POLICY lab_isolation_policy ON ""TestResults""
    USING (
        EXISTS (
            SELECT 1 FROM ""Samples""
            WHERE ""Samples"".""Id"" = ""TestResults"".""SampleId""
        )
    );
");
        }
    }
}
