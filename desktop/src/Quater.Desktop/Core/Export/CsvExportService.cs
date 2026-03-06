using System.Text;
using System.Globalization;
using Quater.Desktop.Api.Model;

namespace Quater.Desktop.Core.Export;

public sealed class CsvExportService : ICsvExportService
{
    public string Export(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        IReadOnlyList<string>? headers = null)
    {
        var resolvedHeaders = ResolveHeaders(rows, headers);
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(',', resolvedHeaders));

        foreach (var row in rows)
        {
            var values = resolvedHeaders
                .Select(header => row.TryGetValue(header, out var value) ? Escape(value) : string.Empty)
                .ToArray();
            builder.AppendLine(string.Join(',', values));
        }

        return builder.ToString();
    }

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

    private static string[] ResolveHeaders(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        IReadOnlyList<string>? headers)
    {
        if (headers is not null)
        {
            return headers.ToArray();
        }

        return rows
            .SelectMany(row => row.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
    }

    private static string Escape(string? value)
    {
        var source = value ?? string.Empty;
        if (!source.Contains(',') && !source.Contains('"') && !source.Contains('\n') && !source.Contains('\r'))
        {
            return source;
        }

        var escaped = source.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
