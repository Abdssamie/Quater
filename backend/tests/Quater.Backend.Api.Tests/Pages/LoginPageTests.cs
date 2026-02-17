using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;
using Xunit.Abstractions;

namespace Quater.Backend.Api.Tests.Pages;

/// <summary>
/// Integration tests for Login Razor Page.
/// Tests timing attack mitigation and security features.
/// </summary>
[Collection("Api")]
public sealed class LoginPageTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private Lab _testLab = null!;

    public LoginPageTests(ApiTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.CreateClient();
        _output = output;
    }

    public async Task InitializeAsync()
    {
        // Reset database and Redis before each test
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();

        // Create test lab
        _testLab = await CreateTestLabAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_InactiveUser_HasSimilarTimingToWrongPassword()
    {
        // Arrange - Create active user
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "active@test.com",
            Email = "active@test.com",
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = _testLab.Id, Role = UserRole.Viewer } ],
            IsActive = true
        };

        await CreateUserAsync(activeUser, "Password123!");

        // Deactivate user
        await SetUserActiveStatusAsync(activeUser.Id, false);

        // Act - Measure timing for inactive user
        var sw1 = Stopwatch.StartNew();
        var response1 = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "active@test.com",
            ["Password"] = "WrongPassword123!"
        }));
        sw1.Stop();

        // Act - Measure timing for wrong password on active user (re-activate first)
        await SetUserActiveStatusAsync(activeUser.Id, true);

        var sw2 = Stopwatch.StartNew();
        var response2 = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "active@test.com",
            ["Password"] = "WrongPassword123!"
        }));
        sw2.Stop();

        // Assert - Timing should be similar (within 100ms)
        var timingDifference = Math.Abs(sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds);
        timingDifference.Should().BeLessThan(100, "timing attack mitigation should make inactive user and wrong password take similar time");
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsGenericError()
    {
        // Arrange
        var inactiveUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "inactive@test.com",
            Email = "inactive@test.com",
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = _testLab.Id, Role = UserRole.Viewer } ],
            IsActive = false
        };

        await CreateUserAsync(inactiveUser, "Password123!");

        // Get antiforgery token
        var (antiforgeryToken, cookies) = await GetAntiforgeryTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "inactive@test.com",
                ["Password"] = "Password123!",
                ["__RequestVerificationToken"] = antiforgeryToken
            })
        };
        if (!string.IsNullOrEmpty(cookies))
        {
            request.Headers.Add("Cookie", cookies);
        }

        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); // Returns page with error
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password"); // Generic error message
    }

    [Fact]
    public async Task Login_ActiveUserCorrectPassword_Succeeds()
    {
        // Arrange
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "active@test.com",
            Email = "active@test.com",
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = _testLab.Id, Role = UserRole.Viewer } ],
            IsActive = true
        };

        await CreateUserAsync(activeUser, "Password123!");

        // Get antiforgery token
        var (antiforgeryToken, cookies) = await GetAntiforgeryTokenAsync();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "active@test.com",
                ["Password"] = "Password123!",
                ["__RequestVerificationToken"] = antiforgeryToken
            })
        };
        if (!string.IsNullOrEmpty(cookies))
        {
            request.Headers.Add("Cookie", cookies);
        }

        var response = await _client.SendAsync(request);

        // Assert - Should redirect (302) or return OK if redirect target doesn't exist (404)
        // In test environment, the redirect target "/" might not exist, so we accept both
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_ExceedsRateLimit_ReturnsError()
    {
        // Arrange
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "ratelimit@test.com",
            Email = "ratelimit@test.com",
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = _testLab.Id, Role = UserRole.Viewer } ],
            IsActive = true,
            LockoutEnabled = true
        };

        await CreateUserAsync(testUser, "Password123!");

        // Act - Attempt login 6 times with wrong password
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 6; i++)
        {
            // Get fresh antiforgery token for each request (since cookies change)
            var (antiforgeryToken, cookies) = await GetAntiforgeryTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Email"] = "ratelimit@test.com",
                    ["Password"] = "WrongPassword!",
                    ["__RequestVerificationToken"] = antiforgeryToken
                })
            };
            if (!string.IsNullOrEmpty(cookies))
            {
                request.Headers.Add("Cookie", cookies);
            }

            var response = await _client.SendAsync(request);
            responses.Add(response);
        }

        // Assert - Defense in depth: Both Identity lockout and rate limiting are active
        // First 2 attempts should return normal error
        for (int i = 0; i < 2; i++)
        {
            responses[i].StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await responses[i].Content.ReadAsStringAsync();
            content.Should().Contain("Invalid email or password");
        }

        // Attempts 3-5 should show account lockout
        for (int i = 2; i < 5; i++)
        {
            responses[i].StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await responses[i].Content.ReadAsStringAsync();
            content.Should().Contain("Account is locked. Please try again later.");
        }

        // 6th attempt should be rate limited (rate limit check happens BEFORE password verification)
        responses[5].StatusCode.Should().Be(HttpStatusCode.OK);
        var rateLimitContent = await responses[5].Content.ReadAsStringAsync();
        rateLimitContent.Should().Contain("Too many login attempts. Please try again in 15 minutes.");
    }

    [Fact]
    public async Task Login_WithoutAntiforgeryToken_ReturnsBadRequest()
    {
        // Arrange
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "csrf@test.com",
            Email = "csrf@test.com",
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = _testLab.Id, Role = UserRole.Viewer } ],
            IsActive = true
        };

        await CreateUserAsync(activeUser, "Password123!");

        // Act - Post without antiforgery token (bypassing the form)
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "csrf@test.com",
                ["Password"] = "Password123!"
            })
        };

        var response = await _client.SendAsync(request);

        // Assert - Should be blocked by CSRF protection (400 Bad Request)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidAntiforgeryToken_Succeeds()
    {
        // Arrange
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "validcsrf@test.com",
            Email = "validcsrf@test.com",
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = _testLab.Id, Role = UserRole.Viewer } ],
            IsActive = true
        };

        await CreateUserAsync(activeUser, "Password123!");

        // First, get the login page to retrieve the antiforgery token
        var getResponse = await _client.GetAsync("/Account/Login");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pageContent = await getResponse.Content.ReadAsStringAsync();

        // Extract the antiforgery token from the page
        var tokenMatch = System.Text.RegularExpressions.Regex.Match(
            pageContent,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]*)""");

        tokenMatch.Success.Should().BeTrue("antiforgery token should be present in the login form");
        var antiforgeryToken = tokenMatch.Groups[1].Value;

        // Act - Post with the antiforgery token
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "validcsrf@test.com",
                ["Password"] = "Password123!",
                ["__RequestVerificationToken"] = antiforgeryToken
            })
        };

        // Copy cookies from the GET request to maintain session
        if (getResponse.Headers.Contains("Set-Cookie"))
        {
            foreach (var cookie in getResponse.Headers.GetValues("Set-Cookie"))
            {
                request.Headers.Add("Cookie", cookie);
            }
        }

        var response = await _client.SendAsync(request);

        // Assert - Should succeed (redirect or OK)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.NotFound);
    }

    #region Helper Methods

    /// <summary>
    /// Gets the antiforgery token from the login page.
    /// </summary>
    private async Task<(string token, string cookies)> GetAntiforgeryTokenAsync()
    {
        var response = await _client.GetAsync("/Account/Login");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        var tokenMatch = Regex.Match(content, @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]*)""");
        tokenMatch.Success.Should().BeTrue("antiforgery token should be present in the login form");

        var cookies = response.Headers.Contains("Set-Cookie")
            ? string.Join("; ", response.Headers.GetValues("Set-Cookie"))
            : string.Empty;
        return (tokenMatch.Groups[1].Value, cookies);
    }

    /// <summary>
    /// Creates a test lab in the database.
    /// </summary>
    private async Task<Lab> CreateTestLabAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();

        var lab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = $"Test Lab {Guid.NewGuid()}",
            Location = "123 Test Street",
            ContactInfo = "test@lab.com",
            IsActive = true
        };

        context.Labs.Add(lab);
        await context.SaveChangesAsync();

        return lab;
    }

    /// <summary>
    /// Creates a user with the specified password.
    /// </summary>
    private async Task CreateUserAsync(User user, string password)
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    /// <summary>
    /// Sets the IsActive status of a user.
    /// </summary>
    private async Task SetUserActiveStatusAsync(Guid userId, bool isActive)
    {
        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();

        var user = await context.Users.FindAsync(userId);
        if (user is not null)
        {
            user.IsActive = isActive;
            await context.SaveChangesAsync();
        }
    }

    #endregion
}
