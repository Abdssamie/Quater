using System.Net;
using System.Net.Http.Json;
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
using Xunit;

namespace Quater.Backend.Api.Tests.Controllers;

/// <summary>
/// Integration tests for RegistrationController.
/// Tests user registration endpoint with various validation scenarios.
/// </summary>
[Collection("Api")]
public sealed class RegistrationControllerTests(ApiTestFixture fixture) : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture = fixture;
    private readonly HttpClient _client = fixture.Client;

    public async Task InitializeAsync()
    {
        // Reset database and Redis before each test
        await _fixture.ResetDatabaseAsync();
        await _fixture.ClearRateLimitKeysAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithUserDetails()
    {
        // Arrange
        var lab = await CreateTestLabAsync();
        var request = new RegisterRequest
        {
            Email = "technician@example.com",
            Password = "SecurePassword123!",
            Role = UserRole.Technician,
            LabId = lab.Id
        };

        // Act
        var response = await _client.PostJsonAsync("/api/registration/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
        content.Should().NotBeNull();
        content!.UserId.Should().NotBeEmpty();
        content.Email.Should().Be(request.Email);
        content.Role.Should().Be(UserRole.Technician.ToString());
        content.Message.Should().Contain("registered successfully");

        // Verify user created in database
        var user = await GetUserByEmailAsync(request.Email);
        user.Should().NotBeNull();
        user!.Email.Should().Be(request.Email);
        user.Role.Should().Be(UserRole.Technician);
        user.LabId.Should().Be(lab.Id);
        user.IsActive.Should().BeTrue();
        user.EmailConfirmed.Should().BeFalse(); // Email not confirmed yet

        // Verify password is hashed (not stored in plain text)
        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.PasswordHash.Should().NotBe(request.Password);

        // Verify email queue was called for verification email
        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Verify(
            x => x.QueueAsync(
                It.Is<EmailQueueItem>(item => item.Email.To == request.Email),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var lab = await CreateTestLabAsync();
        var email = "duplicate@example.com";

        // Create first user
        await CreateTestUserAsync(email, "Password123!", UserRole.Technician, lab.Id);

        // Attempt to register with same email
        var request = new RegisterRequest
        {
            Email = email,
            Password = "DifferentPassword123!",
            Role = UserRole.Admin,
            LabId = lab.Id
        };

        // Act
        var response = await _client.PostJsonAsync("/api/registration/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content.Should().NotBeNull();
        content!.Errors.Should().NotBeNull();
        var errorMessage = content.Errors!.First();
        errorMessage.Should().Match(msg => 
            msg.Contains("already taken", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("exists", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var lab = await CreateTestLabAsync();
        var request = new RegisterRequest
        {
            Email = "not-an-email",
            Password = "SecurePassword123!",
            Role = UserRole.Technician,
            LabId = lab.Id
        };

        // Act
        var response = await _client.PostJsonAsync("/api/registration/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        content.Should().NotBeNull();
        content!.Errors.Should().ContainKey("Email")
            .WhoseValue.Should().Contain(error => error.Contains("email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        // Arrange
        var lab = await CreateTestLabAsync();
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "short", // Less than 8 characters
            Role = UserRole.Technician,
            LabId = lab.Id
        };

        // Act
        var response = await _client.PostJsonAsync("/api/registration/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        content.Should().NotBeNull();
        content!.Errors.Should().ContainKey("Password")
            .WhoseValue.Should().Contain(error => 
                error.Contains("8 characters", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("at least 8", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_MissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var lab = await CreateTestLabAsync();
        var request = new RegisterRequest
        {
            Email = string.Empty,
            Password = "SecurePassword123!",
            Role = UserRole.Technician,
            LabId = lab.Id
        };

        // Act
        var response = await _client.PostJsonAsync("/api/registration/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        content.Should().NotBeNull();
        content!.Errors.Should().ContainKey("Email")
            .WhoseValue.Should().Contain(error => error.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_MissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var lab = await CreateTestLabAsync();
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = string.Empty,
            Role = UserRole.Technician,
            LabId = lab.Id
        };

        // Act
        var response = await _client.PostJsonAsync("/api/registration/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        content.Should().NotBeNull();
        content!.Errors.Should().ContainKey("Password")
            .WhoseValue.Should().Contain(error => error.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_MissingLabId_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "SecurePassword123!",
            Role = UserRole.Technician,
            LabId = Guid.Empty
        };

        // Act
        var response = await _client.PostJsonAsync("/api/registration/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        content.Should().NotBeNull();
        content!.Errors.Should().ContainKey("LabId")
            .WhoseValue.Should().Contain(error => error.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_NonExistentLabId_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentLabId = Guid.NewGuid();
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "SecurePassword123!",
            Role = UserRole.Technician,
            LabId = nonExistentLabId
        };

        // Act
        var response = await _client.PostJsonAsync("/api/registration/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        content.Should().NotBeNull();
        // Identity will fail to create user due to foreign key constraint
        content!.Errors.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_AllRoles_CreatesUsersWithCorrectRoles()
    {
        // Arrange
        var lab = await CreateTestLabAsync();
        var roles = new[] { UserRole.Admin, UserRole.Technician, UserRole.Viewer };

        foreach (var role in roles)
        {
            var request = new RegisterRequest
            {
                Email = $"{role.ToString().ToLower()}@example.com",
                Password = "SecurePassword123!",
                Role = role,
                LabId = lab.Id
            };

            // Act
            var response = await _client.PostJsonAsync("/api/registration/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await GetUserByEmailAsync(request.Email);
            user.Should().NotBeNull();
            user!.Role.Should().Be(role);
        }
    }

    [Fact]
    public async Task Register_MultipleUsers_AllReceiveVerificationEmails()
    {
        // Arrange
        var lab = await CreateTestLabAsync();
        var users = new[]
        {
            new RegisterRequest
            {
                Email = "user1@example.com",
                Password = "SecurePassword123!",
                Role = UserRole.Technician,
                LabId = lab.Id
            },
            new RegisterRequest
            {
                Email = "user2@example.com",
                Password = "SecurePassword123!",
                Role = UserRole.Viewer,
                LabId = lab.Id
            }
        };

        // Clear mock invocations before test to avoid counting invocations from other tests
        var emailQueue = _fixture.Services.GetRequiredService<IEmailQueue>();
        var mockEmailQueue = Mock.Get(emailQueue);
        mockEmailQueue.Invocations.Clear();

        // Act
        foreach (var request in users)
        {
            var response = await _client.PostJsonAsync("/api/registration/register", request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Assert - Verify email queue was called for each user
        mockEmailQueue.Verify(
            x => x.QueueAsync(
                It.IsAny<EmailQueueItem>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(users.Length));
    }

    #region Helper Methods

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
    /// Creates a test user in the database.
    /// </summary>
    private async Task<User> CreateTestUserAsync(string email, string password, UserRole role, Guid labId)
    {
        using var scope = _fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = new User
        {
            UserName = email,
            Email = email,
            Role = role,
            LabId = labId,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    /// <summary>
    /// Gets a user by email from the database.
    /// </summary>
    private async Task<User?> GetUserByEmailAsync(string email)
    {
        using var scope = _fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuaterDbContext>();

        return await context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    #endregion

    #region Response Models

    private sealed class RegistrationResponse
    {
        public string Message { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    private sealed class ErrorResponse
    {
        public string[]? Errors { get; set; }
    }

    private sealed class ValidationErrorResponse
    {
        public Dictionary<string, string[]> Errors { get; set; } = [];
    }

    #endregion
}
