using Quater.Desktop.Api.Model;

namespace Quater.Desktop.Core.Export;

public interface ICsvExportService
{
    string ExportAuditLogs(IReadOnlyList<AuditLogDto> auditLogs);
}
