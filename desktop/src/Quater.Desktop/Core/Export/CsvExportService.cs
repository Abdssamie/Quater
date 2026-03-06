using System.Text;

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
