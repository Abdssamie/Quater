using Quater.Desktop.Api.Model;
namespace Quater.Desktop.Core.Export;

public interface ICsvExportService
{
    string Export(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        IReadOnlyList<string>? headers = null);
    string ExportAuditLogs(IReadOnlyList<AuditLogDto> auditLogs);
}
