using System.Globalization;
using System.Text;
using Quater.Desktop.Api.Model;

namespace Quater.Desktop.Core.Export;

public sealed class CsvExportService : ICsvExportService
{
    public string ExportAuditLogs(IReadOnlyList<AuditLogDto> auditLogs)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Id,Timestamp,UserId,UserEmail,EntityType,EntityId,Action,IpAddress,IsArchived");

        foreach (var log in auditLogs)
        {
            builder.Append(Escape(log.Id.ToString()));
            builder.Append(',');
            builder.Append(Escape(log.Timestamp.ToString("O", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(Escape(log.UserId.ToString()));
            builder.Append(',');
            builder.Append(Escape(log.UserEmail));
            builder.Append(',');
            builder.Append(Escape(log.EntityType?.ToString() ?? string.Empty));
            builder.Append(',');
            builder.Append(Escape(log.EntityId.ToString()));
            builder.Append(',');
            builder.Append(Escape(log.Action?.ToString() ?? string.Empty));
            builder.Append(',');
            builder.Append(Escape(log.IpAddress));
            builder.Append(',');
            builder.Append(log.IsArchived ? "true" : "false");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
