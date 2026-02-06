using System.Net;
using Quater.Backend.Api.Tests.Fixtures;
using Xunit;

namespace Quater.Backend.Api.Tests.Authorization;

/// <summary>
/// Tests for fallback authorization policy that requires authentication by default.
/// Verifies that endpoints without [AllowAnonymous] require authentication.
/// </summary>
public class FallbackPolicyTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public FallbackPolicyTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.Client;
    }

    /// <summary>
    /// Test that protected endpoints (without [AllowAnonymous]) return 401 Unauthorized
    /// when accessed without authentication.
    /// </summary>
    [Fact]
    public async Task ProtectedEndpoint_WithoutAuthentication_Returns401()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();

        // Act - Try to access /api/samples without authentication
        var response = await _client.GetAsync("/api/samples");

        // Assert - Should return 401 Unauthorized due to fallback policy
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test that endpoints with [AllowAnonymous] are accessible without authentication.
    /// Health check endpoints should remain publicly accessible.
    /// </summary>
    [Fact]
    public async Task AnonymousEndpoint_WithoutAuthentication_ReturnsSuccess()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();

        // Act - Try to access /api/health without authentication
        var response = await _client.GetAsync("/api/health");

        // Assert - Should return 200 OK because [AllowAnonymous] is present
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Test that health check liveness endpoint is accessible without authentication.
    /// </summary>
    [Fact]
    public async Task HealthLivenessEndpoint_WithoutAuthentication_ReturnsSuccess()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();

        // Act - Try to access /api/health/live without authentication
        var response = await _client.GetAsync("/api/health/live");

        // Assert - Should return 200 OK because [AllowAnonymous] is present
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Test that health check readiness endpoint is accessible without authentication.
    /// </summary>
    [Fact]
    public async Task HealthReadinessEndpoint_WithoutAuthentication_ReturnsSuccess()
    {
        // Arrange
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();

        // Act - Try to access /api/health/ready without authentication
        var response = await _client.GetAsync("/api/health/ready");

        // Assert - Should return 200 OK because [AllowAnonymous] is present
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
