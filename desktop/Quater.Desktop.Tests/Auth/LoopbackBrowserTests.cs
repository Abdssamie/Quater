using Duende.IdentityModel.OidcClient.Browser;
using Quater.Desktop.Core.Auth.Browser;

namespace Quater.Desktop.Tests.Auth;

public sealed class LoopbackBrowserTests
{
    [Fact]
    public async Task InvokeAsync_WhenCancellationRequestedBeforeContext_ReturnsUserCancel()
    {
        // Use an ephemeral port with a callback path
        var browser = new LoopbackBrowser("http://127.0.0.1:17234/auth/callback");
        var options = new BrowserOptions("https://example.com/auth", "http://127.0.0.1:17234/auth/callback");

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // pre-cancel before calling InvokeAsync

        var result = await browser.InvokeAsync(options, cts.Token);

        Assert.Equal(BrowserResultType.UserCancel, result.ResultType);
    }

    [Fact]
    public async Task InvokeAsync_WhenCancellationRequestedDuringWait_ReturnsUserCancel()
    {
        var browser = new LoopbackBrowser("http://127.0.0.1:17235/auth/callback");
        var options = new BrowserOptions("https://example.com/auth", "http://127.0.0.1:17235/auth/callback");

        using var cts = new CancellationTokenSource();
        // Cancel shortly after InvokeAsync starts waiting
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var result = await browser.InvokeAsync(options, cts.Token);

        Assert.Equal(BrowserResultType.UserCancel, result.ResultType);
    }
}
