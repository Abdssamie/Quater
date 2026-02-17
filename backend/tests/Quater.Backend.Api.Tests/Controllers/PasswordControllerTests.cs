using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenIddict.Abstractions;
using Quater.Backend.Api.Tests.Fixtures;
using Quater.Backend.Api.Tests.Helpers;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Api.Tests.Controllers;

/// <summary>
/// Integration tests for PasswordController endpoints.
/// Tests password change, forgot password, and reset password functionality.
/// </summary>
[Collection("Api")]
public sealed class PasswordControllerTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture = fixture;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();

        // Seed OpenIddict client for auth code flow
        using var seedScope = _fixture.Services.CreateScope();
        var appManager = seedScope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await appManager.FindByClientIdAsync("quater-mobile-client") is null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "quater-mobile-client",
                DisplayName = "Quater Mobile/Desktop Client",
                ClientType = OpenIddictConstants.ClientTypes.Public,
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                RedirectUris = { new Uri("quater://oauth/callback"), new Uri("http://127.0.0.1/callback") },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Revocation,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "api",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access"
                },
                Requirements = { OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange }
            });
        }

        _client = _fixture.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        return Task.CompletedTask;
    }

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var (user, password) = await CreateTestUserAsync("change-valid@test.com", "OldPassword123!");
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, password);
        _client.AddAuthToken(tokenResponse.AccessToken);

        var request = new
        {
            CurrentPassword = password,
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/password/change", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<MessageResponse>();
        content.Should().NotBeNull();
        content!.Message.Should().Be("Password changed successfully");

        // Verify new password works
        _client.RemoveAuthToken();
        var newTokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, "NewPassword456!");
        newTokenResponse.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        var (user, password) = await CreateTestUserAsync("change-wrong@test.com", "OldPassword123!");
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, password);
        _client.AddAuthToken(tokenResponse.AccessToken);

        var request = new
        {
            CurrentPassword = "WrongPassword999!",
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/password/change", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Incorrect password");
    }

    [Fact]
    public async Task ChangePassword_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword456!"
        };

        // Act (no auth token)
        var response = await _client.PostJsonAsync("/api/password/change", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_ReturnsBadRequest()
    {
        // Arrange
        var (user, password) = await CreateTestUserAsync("change-weak@test.com", "OldPassword123!");
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, password);
        _client.AddAuthToken(tokenResponse.AccessToken);

        var request = new
        {
            CurrentPassword = password,
            NewPassword = "weak" // Less than 8 characters
        };

        // Act
        var response = await _client.PostJsonAsync("/api/password/change", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("at least 8 characters");
    }

    [Fact]
    public async Task ChangePassword_MissingCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        var (user, password) = await CreateTestUserAsync("change-missing@test.com", "OldPassword123!");
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, password);
        _client.AddAuthToken(tokenResponse.AccessToken);

        var request = new
        {
            CurrentPassword = "", // Empty
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/password/change", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPassword_ExistingEmail_ReturnsOkAndQueuesEmail()
    {
        // Arrange
        var (user, _) = await CreateTestUserAsync("forgot-exists@test.com", "Password123!");
        var request = new { Email = user.Email! };

        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Invocations.Clear();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.PostJsonAsync("/api/password/forgot", request);
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<MessageResponse>();
        content.Should().NotBeNull();
        content!.Message.Should().Be("If the email exists, a password reset link has been sent");

        // Verify email was queued
        mockEmailQueue.Verify(
            x => x.QueueAsync(It.IsAny<EmailQueueItem>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify timing attack protection (200ms delay)
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(200);
    }

    [Fact]
    public async Task ForgotPassword_NonExistentEmail_ReturnsOkWithoutQueuingEmail()
    {
        // Arrange
        var request = new { Email = "nonexistent@test.com" };

        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Invocations.Clear();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.PostJsonAsync("/api/password/forgot", request);
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<MessageResponse>();
        content.Should().NotBeNull();
        content!.Message.Should().Be("If the email exists, a password reset link has been sent");

        // Verify email was NOT queued (email enumeration protection)
        mockEmailQueue.Verify(
            x => x.QueueAsync(It.IsAny<EmailQueueItem>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify timing attack protection (200ms delay)
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(200);
    }

    [Fact]
    public async Task ForgotPassword_InactiveUser_ReturnsOkWithoutQueuingEmail()
    {
        // Arrange
        var (user, _) = await CreateTestUserAsync("forgot-inactive@test.com", "Password123!", isActive: false);
        var request = new { Email = user.Email! };

        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Invocations.Clear();

        // Act
        var response = await _client.PostJsonAsync("/api/password/forgot", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<MessageResponse>();
        content.Should().NotBeNull();
        content!.Message.Should().Be("If the email exists, a password reset link has been sent");

        // Verify email was NOT queued for inactive user
        mockEmailQueue.Verify(
            x => x.QueueAsync(It.IsAny<EmailQueueItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new { Email = "not-an-email" };

        // Act
        var response = await _client.PostJsonAsync("/api/password/forgot", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_TimingAttackProtection_ConsistentResponseTime()
    {
        // Arrange
        var (user, _) = await CreateTestUserAsync("timing-exists@test.com", "Password123!");
        
        var existingEmailRequest = new { Email = user.Email! };
        var nonExistentEmailRequest = new { Email = "nonexistent@test.com" };

        // Act - Test existing email
        var stopwatch1 = Stopwatch.StartNew();
        var response1 = await _client.PostJsonAsync("/api/password/forgot", existingEmailRequest);
        stopwatch1.Stop();

        // Act - Test non-existent email
        var stopwatch2 = Stopwatch.StartNew();
        var response2 = await _client.PostJsonAsync("/api/password/forgot", nonExistentEmailRequest);
        stopwatch2.Stop();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Response times should be consistent (within 300ms) to prevent timing attacks
        // Note: Using a wider tolerance for CI/test environments where scheduling jitter is common
        var timeDifference = Math.Abs(stopwatch1.ElapsedMilliseconds - stopwatch2.ElapsedMilliseconds);
        timeDifference.Should().BeLessThan(300, "response times should be consistent to prevent timing attacks");

        // Both should have the 200ms delay
        stopwatch1.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(200);
        stopwatch2.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(200);
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsOkAndSendsSecurityAlert()
    {
        // Arrange
        var (user, _) = await CreateTestUserAsync("reset-valid@test.com", "OldPassword123!");
        var resetToken = await GeneratePasswordResetTokenAsync(user);

        var request = new
        {
            Email = user.Email!,
            Code = resetToken,
            NewPassword = "NewPassword456!"
        };

        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Invocations.Clear();

        // Act
        var response = await _client.PostJsonAsync("/api/password/reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<MessageResponse>();
        content.Should().NotBeNull();
        content!.Message.Should().Be("Password reset successfully");

        // Verify security alert email was queued
        mockEmailQueue.Verify(
            x => x.QueueAsync(It.IsAny<EmailQueueItem>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify new password works
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, "NewPassword456!");
        tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var (user, _) = await CreateTestUserAsync("reset-invalid@test.com", "OldPassword123!");

        var request = new
        {
            Email = user.Email!,
            Code = "invalid-token-12345",
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/password/reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid or expired reset token");
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_ReturnsBadRequest()
    {
        // Arrange
        var (user, _) = await CreateTestUserAsync("reset-expired@test.com", "OldPassword123!");
        
        var resetToken = await GeneratePasswordResetTokenAsync(user);
        // Invalidate the token by updating security stamp
        await InvalidateUserTokensAsync(user);

        var request = new
        {
            Email = user.Email!,
            Code = resetToken,
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/password/reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid or expired reset token");
    }

    [Fact]
    public async Task ResetPassword_WeakNewPassword_ReturnsBadRequest()
    {
        // Arrange
        var (user, _) = await CreateTestUserAsync("reset-weak@test.com", "OldPassword123!");
        var resetToken = await GeneratePasswordResetTokenAsync(user);

        var request = new
        {
            Email = user.Email!,
            Code = resetToken,
            NewPassword = "weak" // Less than 8 characters
        };

        // Act
        var response = await _client.PostJsonAsync("/api/password/reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("at least 8 characters");
    }

    [Fact]
    public async Task ResetPassword_NonExistentEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent@test.com",
            Code = "some-token",
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/password/reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid request");
    }

    [Fact]
    public async Task ResetPassword_SecurityAlertEmailFailure_StillSucceeds()
    {
        // Arrange
        var (user, _) = await CreateTestUserAsync("reset-email-fail@test.com", "OldPassword123!");
        var resetToken = await GeneratePasswordResetTokenAsync(user);

        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Setup(x => x.QueueAsync(It.IsAny<EmailQueueItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email service unavailable"));

        var request = new
        {
            Email = user.Email!,
            Code = resetToken,
            NewPassword = "NewPassword456!"
        };

        // Act
        var response = await _client.PostJsonAsync("/api/password/reset", request);

        // Assert - Password reset should still succeed even if email fails
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<MessageResponse>();
        content.Should().NotBeNull();
        content!.Message.Should().Be("Password reset successfully");

        // Verify new password works
        var tokenResponse = await AuthenticationHelper.GetAuthTokenViaAuthCodeFlowAsync(_fixture, user.Email!, "NewPassword456!");
        tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ResetPassword_NonExistentEmail_ReturnsBadRequestWithTimingProtection()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent-reset@test.com",
            Code = "some-token",
            NewPassword = "NewPassword456!"
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.PostJsonAsync("/api/password/reset", request);
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid request");

        // Verify timing attack protection (200ms delay)
        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(200);
    }

    [Fact]
    public async Task ResetPassword_TimingAttackProtection_ConsistentResponseTime()
    {
        // Arrange
        var (user, _) = await CreateTestUserAsync("timing-reset-exists@test.com", "OldPassword123!");
        var resetToken = await GeneratePasswordResetTokenAsync(user);

        var existingEmailRequest = new
        {
            Email = user.Email!,
            Code = resetToken,
            NewPassword = "NewPassword456!"
        };

        var nonExistentEmailRequest = new
        {
            Email = "nonexistent-reset2@test.com",
            Code = "some-token",
            NewPassword = "NewPassword456!"
        };

        // Act - Test existing email
        var stopwatch1 = Stopwatch.StartNew();
        var response1 = await _client.PostJsonAsync("/api/password/reset", existingEmailRequest);
        stopwatch1.Stop();

        // Act - Test non-existent email
        var stopwatch2 = Stopwatch.StartNew();
        var response2 = await _client.PostJsonAsync("/api/password/reset", nonExistentEmailRequest);
        stopwatch2.Stop();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Response times should be consistent (within 300ms) to prevent timing attacks
        var timeDifference = Math.Abs(stopwatch1.ElapsedMilliseconds - stopwatch2.ElapsedMilliseconds);
        timeDifference.Should().BeLessThan(300, "response times should be consistent to prevent timing attacks");

        // Both should have at least some processing time
        stopwatch1.ElapsedMilliseconds.Should().BeGreaterThan(0);
        stopwatch2.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(200);
    }

    #endregion

    #region Helper Methods

    private async Task<(User user, string password)> CreateTestUserAsync(
        string email,
        string password,
        bool isActive = true)
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();

        // Create a test lab
        var lab = new Lab
        {
            Id = Guid.NewGuid(),
            Name = $"Test Lab for {email}",
            Location = "123 Test St, Test City, Test Country",
            ContactInfo = "test@lab.com",
            IsActive = true
        };
        dbContext.Labs.Add(lab);
        await dbContext.SaveChangesAsync();

        // Create test user
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = lab.Id, Role = UserRole.Admin } ],
            IsActive = isActive
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return (user, password);
    }

    private async Task<string> GeneratePasswordResetTokenAsync(User user)
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    private async Task InvalidateUserTokensAsync(User user)
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        // Re-fetch user in this scope to avoid entity tracking conflicts
        var freshUser = await userManager.FindByIdAsync(user.Id.ToString());
        await userManager.UpdateSecurityStampAsync(freshUser!);
    }

    #endregion
}

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}
