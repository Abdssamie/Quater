using Quater.Desktop.Api.Client;
using Quater.Desktop.Core.Api;

namespace Quater.Desktop.Tests.Core.Api;

public sealed class ApiErrorFormatterTests
{
    [Theory]
    [InlineData(401, "Session expired")]
    [InlineData(403, "Permission denied")]
    [InlineData(400, "Invalid request")]
    public void Format_WhenApiExceptionHasKnownStatusCode_ReturnsFriendlyMessage(int statusCode, string expected)
    {
        var formatter = new ApiErrorFormatter();
        var exception = new ApiException(statusCode, "api error");

        var actual = formatter.Format(exception, "Fallback message");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Format_WhenApiExceptionHasUnknownStatusCode_ReturnsFallbackMessage()
    {
        var formatter = new ApiErrorFormatter();
        var exception = new ApiException(500, "server exploded");

        var actual = formatter.Format(exception, "Fallback message");

        Assert.Equal("Fallback message", actual);
    }
}
