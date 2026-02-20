using Duende.IdentityModel.OidcClient.Browser;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Quater.Desktop.Core.Auth.Browser;

public sealed class LoopbackBrowser : IBrowser
{
    private readonly int _port;

    public LoopbackBrowser(string url)
    {
        var uri = new Uri(url);
        _port = uri.Port;
        CallbackPath = uri.AbsolutePath.TrimEnd('/') + "/";
    }

    private string CallbackPath { get; }

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken ct = default)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{_port}{CallbackPath}");
        listener.Start();

        Process.Start(new ProcessStartInfo
        {
            FileName = options.StartUrl,
            UseShellExecute = true
        });

        var context = await listener.GetContextAsync();
        var response = context.Response;
        var responseHtml = "<html><head><title>Quater</title></head><body>Authentication complete. You can close this window.</body></html>";
        var buffer = Encoding.UTF8.GetBytes(responseHtml);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html";
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, ct);
        response.OutputStream.Close();

        var result = context.Request.Url?.ToString() ?? string.Empty;
        return new BrowserResult
        {
            ResultType = BrowserResultType.Success,
            Response = result
        };
    }
}
