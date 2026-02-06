using System.Text;
using System.Text.Json;

namespace Quater.Backend.Api.Tests.Helpers;

/// <summary>
/// Extension methods for HttpClient to simplify JSON operations.
/// </summary>
public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Sends a GET request and deserializes the JSON response.
    /// </summary>
    public static async Task<T?> GetJsonAsync<T>(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>
    /// Sends a POST request with JSON content.
    /// </summary>
    public static async Task<HttpResponseMessage> PostJsonAsync<T>(
        this HttpClient client,
        string url,
        T data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content);
    }

    /// <summary>
    /// Sends a PUT request with JSON content.
    /// </summary>
    public static async Task<HttpResponseMessage> PutJsonAsync<T>(
        this HttpClient client,
        string url,
        T data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PutAsync(url, content);
    }

    /// <summary>
    /// Sets the X-Forwarded-For header to simulate requests from different IP addresses.
    /// </summary>
    public static void SetForwardedFor(this HttpClient client, string ipAddress)
    {
        client.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);
    }

    /// <summary>
    /// Deserializes error response from API.
    //
    public static async Task<ErrorResponse?> GetErrorResponseAsync(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ErrorResponse>(json, JsonOptions);
    }
}

public class ErrorResponse
{
    public string? Error { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}
