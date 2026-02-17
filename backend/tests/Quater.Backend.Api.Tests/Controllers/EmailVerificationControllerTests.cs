using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Quater.Backend.Api.Controllers;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Api.Tests.Controllers;

/// <summary>
/// Integration tests for EmailVerificationController.
/// Tests the new email verification endpoints that replaced the old auth endpoints.
/// </summary>
[Collection("Api")]
public sealed class EmailVerificationControllerTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture = fixture;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();
        _client = _fixture.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region VerifyEmail Tests

    [Fact]
    public async Task VerifyEmail_WithValidToken_SetsEmailConfirmedToTrue()
    {
        // Arrange
        var (user, token) = await CreateUnverifiedUserAsync();

        var request = new VerifyEmailRequest
        {
            UserId = user.Id.ToString(),
            Code = token
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.GetProperty("message").GetString().Should().Be("Email verified successfully");

        // Verify database state
        var updatedUser = await GetUserByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyEmail_WithValidToken_QueuesWelcomeEmail()
    {
        // Arrange
        var (user, token) = await CreateUnverifiedUserAsync();

        var request = new VerifyEmailRequest
        {
            UserId = user.Id.ToString(),
            Code = token
        };

        // Get the mocked email queue to verify it was called
        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Invocations.Clear(); // Clear any previous invocations

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify welcome email was queued
        mockEmailQueue.Verify(
            x => x.QueueAsync(
                It.Is<EmailQueueItem>(item =>
                    item.Email.To == user.Email &&
                    item.Email.Subject.Contains("Welcome")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var (user, _) = await CreateUnverifiedUserAsync();

        var request = new VerifyEmailRequest
        {
            UserId = user.Id.ToString(),
            Code = "invalid-token-12345"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.GetProperty("error").GetString().Should().Be("Invalid or expired verification code");

        // Verify email is still not confirmed
        var updatedUser = await GetUserByIdAsync(user.Id);
        updatedUser!.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmail_WithExpiredToken_ReturnsBadRequest()
    {
        // Arrange
        var (user, _) = await CreateUnverifiedUserAsync();

        // Create an expired token by using a token from a different user
        // (simulates expired/invalid token scenario)
        var expiredToken = "CfDJ8ExpiredTokenThatWillNotWork123456789";

        var request = new VerifyEmailRequest
        {
            UserId = user.Id.ToString(),
            Code = expiredToken
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.GetProperty("error").GetString().Should().Be("Invalid or expired verification code");

        // Verify email is still not confirmed
        var updatedUser = await GetUserByIdAsync(user.Id);
        updatedUser!.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmail_WithAlreadyVerifiedEmail_ReturnsOkWithMessage()
    {
        // Arrange
        var (user, token) = await CreateUnverifiedUserAsync();

        // First verification
        var firstRequest = new VerifyEmailRequest
        {
            UserId = user.Id.ToString(),
            Code = token
        };
        await _client.PostJsonAsync("/api/email-verification/verify", firstRequest);

        // Second verification attempt
        var secondRequest = new VerifyEmailRequest
        {
            UserId = user.Id.ToString(),
            Code = token
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/verify", secondRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.GetProperty("message").GetString().Should().Be("Email already verified");
    }

    [Fact]
    public async Task VerifyEmail_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var request = new VerifyEmailRequest
        {
            UserId = nonExistentUserId.ToString(),
            Code = "some-token"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.GetProperty("error").GetString().Should().Be("User not found");
    }

    [Fact]
    public async Task VerifyEmail_WithMissingUserId_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyEmailRequest
        {
            UserId = string.Empty,
            Code = "some-token"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_WithMissingCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyEmailRequest
        {
            UserId = Guid.NewGuid().ToString(),
            Code = string.Empty
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region ResendVerification Tests

    [Fact]
    public async Task ResendVerification_WithValidUnverifiedEmail_ReturnsOkAndQueuesEmail()
    {
        // Arrange
        var (user, _) = await CreateUnverifiedUserAsync();

        var request = new ResendVerificationRequest
        {
            Email = user.Email!
        };

        // Get the mocked email queue to verify it was called
        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Invocations.Clear(); // Clear any previous invocations

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/resend", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.GetProperty("message").GetString()
            .Should().Be("If the email exists and is not verified, a verification link has been sent");

        // Verify verification email was queued
        mockEmailQueue.Verify(
            x => x.QueueAsync(
                It.Is<EmailQueueItem>(item =>
                    item.Email.To == user.Email &&
                    item.Email.Subject.Contains("Verify")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendVerification_WithNonExistentEmail_ReturnsOkWithoutQueuingEmail()
    {
        // Arrange
        var request = new ResendVerificationRequest
        {
            Email = "nonexistent@example.com"
        };

        // Get the mocked email queue to verify it was NOT called
        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Invocations.Clear(); // Clear any previous invocations

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/resend", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.GetProperty("message").GetString()
            .Should().Be("If the email exists and is not verified, a verification link has been sent");

        // Verify NO email was queued (prevents email enumeration)
        mockEmailQueue.Verify(
            x => x.QueueAsync(
                It.IsAny<EmailQueueItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendVerification_WithAlreadyVerifiedEmail_ReturnsOkWithoutQueuingEmail()
    {
        // Arrange
        var (user, token) = await CreateUnverifiedUserAsync();

        // Verify the email first
        var verifyRequest = new VerifyEmailRequest
        {
            UserId = user.Id.ToString(),
            Code = token
        };
        await _client.PostJsonAsync("/api/email-verification/verify", verifyRequest);

        // Get the mocked email queue and clear invocations
        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Invocations.Clear(); // Clear previous invocations

        // Try to resend verification
        var resendRequest = new ResendVerificationRequest
        {
            Email = user.Email!
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/resend", resendRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.GetProperty("message").GetString()
            .Should().Be("If the email exists and is not verified, a verification link has been sent");

        // Verify NO email was queued (already verified)
        mockEmailQueue.Verify(
            x => x.QueueAsync(
                It.IsAny<EmailQueueItem>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendVerification_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new ResendVerificationRequest
        {
            Email = "not-an-email"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/resend", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResendVerification_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new ResendVerificationRequest
        {
            Email = string.Empty
        };

        // Act
        var response = await _client.PostJsonAsync("/api/email-verification/resend", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task VerifyEmail_ExceedsRateLimit_Returns429()
    {
        // Arrange - Create a user for verification
        var (user, _) = await CreateUnverifiedUserAsync();

        // Make 11 requests (limit is 10 per 60 minutes per IP)
        for (int i = 0; i < 10; i++)
        {
            var request = new VerifyEmailRequest
            {
                UserId = user.Id.ToString(),
                Code = "invalid-token"
            };
            await _client.PostJsonAsync("/api/email-verification/verify", request);
        }

        // Act - 11th request should be rate limited
        var finalRequest = new VerifyEmailRequest
        {
            UserId = user.Id.ToString(),
            Code = "invalid-token"
        };
        var response = await _client.PostJsonAsync("/api/email-verification/verify", finalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ResendVerification_ExceedsRateLimit_Returns429()
    {
        // Arrange - Make 6 requests with same email (limit is 5 per 60 minutes per email)
        for (int i = 0; i < 5; i++)
        {
            var request = new ResendVerificationRequest
            {
                Email = "test-rate-limit@example.com"
            };
            await _client.PostJsonAsync("/api/email-verification/resend", request);
        }

        // Act - 6th request should be rate limited
        var finalRequest = new ResendVerificationRequest
        {
            Email = "test-rate-limit@example.com"
        };
        var response = await _client.PostJsonAsync("/api/email-verification/resend", finalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test user with an unverified email and returns the user and verification token.
    /// </summary>
    private async Task<(User user, string token)> CreateUnverifiedUserAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();

        // Create a test lab first
        var lab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = $"Test Lab {Guid.NewGuid()}",
            Location = "123 Test St, Test City, Test Country",
            ContactInfo = "test@lab.com",
            IsActive = true
        };
        dbContext.Labs.Add(lab);
        await dbContext.SaveChangesAsync();

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = $"testuser{Guid.NewGuid()}@example.com",
            Email = $"testuser{Guid.NewGuid()}@example.com",
            EmailConfirmed = false,
            UserLabs = [ new UserLab { LabId = lab.Id, Role = UserRole.Technician } ],
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, "Test@123456789");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Generate email confirmation token
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        return (user, token);
    }

    /// <summary>
    /// Gets a user by ID from the database.
    /// </summary>
    private async Task<User?> GetUserByIdAsync(Guid userId)
    {
        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();

        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    #endregion
}
