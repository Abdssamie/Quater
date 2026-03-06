using Quater.Desktop.Core.Export;

namespace Quater.Desktop.Tests.Core.Export;

public sealed class CsvExportServiceTests
{
    [Fact]
    public void Export_WhenRowsContainDifferentKeys_UsesStableAlphabeticalHeaderOrdering()
    {
        var service = new CsvExportService();
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows =
        [
            new Dictionary<string, string>
            {
                ["Name"] = "Alpha",
                ["Action"] = "Create"
            },
            new Dictionary<string, string>
            {
                ["Timestamp"] = "2026-03-06T09:00:00Z",
                ["Name"] = "Beta"
            }
        ];

        var csv = service.Export(rows);

        Assert.Equal("Action,Name,Timestamp\nCreate,Alpha,\n,Beta,2026-03-06T09:00:00Z\n", csv);
    }

    [Fact]
    public void Export_WhenValuesContainCommaQuoteOrNewLine_EscapesAccordingToCsvRules()
    {
        var service = new CsvExportService();
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows =
        [
            new Dictionary<string, string>
            {
                ["Action"] = "Create,Update",
                ["Name"] = "A \"quoted\" value",
                ["Notes"] = "Line one\nLine two"
            }
        ];

        var csv = service.Export(rows);

        Assert.Equal("Action,Name,Notes\n\"Create,Update\",\"A \"\"quoted\"\" value\",\"Line one\nLine two\"\n", csv);
    }

    [Fact]
    public void Export_WhenInputIsEmpty_ReturnsHeaderOnlyLine()
    {
        var service = new CsvExportService();

        var csv = service.Export([], ["Action", "Name"]);

        Assert.Equal("Action,Name\n", csv);
    }
}
