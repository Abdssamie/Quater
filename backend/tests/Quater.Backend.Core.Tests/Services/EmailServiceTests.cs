using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Services;
using Xunit;

namespace Quater.Backend.Core.Tests.Services;

public sealed class EmailServiceTests
{
    private readonly Mock<ILogger<SmtpEmailService>> _loggerMock;
    private readonly Mock<ILogger<ScribanEmailTemplateService>> _templateLoggerMock;
    private readonly Mock<IEmailTemplateService> _templateServiceMock;
    private readonly IConfiguration _configuration;

    public EmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<SmtpEmailService>>();
        _templateLoggerMock = new Mock<ILogger<ScribanEmailTemplateService>>();
        _templateServiceMock = new Mock<IEmailTemplateService>();

        // Create in-memory configuration
        var configDict = new Dictionary<string, string?>
        {
            ["Email:Smtp:Host"] = "localhost",
            ["Email:Smtp:Port"] = "1025",
            ["Email:Smtp:EnableSsl"] = "false",
            ["Email:Smtp:Username"] = "",
            ["Email:Smtp:Password"] = "",
            ["Email:From:Address"] = "noreply@quater.app",
            ["Email:From:Name"] = "Quater Water Quality",
            ["Email:BaseUrl"] = "http://localhost:5000"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    #region DTO Tests

    [Fact]
    public void SendEmailDto_RequiredProperties_AreEnforced()
    {
        // Arrange & Act
        var dto = new SendEmailDto
        {
            To = "test@example.com",
            Subject = "Test Subject",
            Body = "<h1>Test Body</h1>"
        };

        // Assert
        dto.To.Should().Be("test@example.com");
        dto.Subject.Should().Be("Test Subject");
        dto.Body.Should().Be("<h1>Test Body</h1>");
        dto.IsHtml.Should().BeTrue(); // Default value
        dto.ReplyTo.Should().BeNull();
    }

    [Fact]
    public void EmailQueueItem_WithParameters_CreatesCorrectly()
    {
        // Arrange
        var email = new SendEmailDto
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Test body"
        };

        // Act
        var item = new EmailQueueItem(email);

        // Assert
        item.Email.Should().Be(email);
        item.RetryCount.Should().Be(0);
        item.ScheduledAt.Should().BeNull();
    }

    [Fact]
    public void EmailQueueItem_WithRetryCount_CreatesCorrectly()
    {
        // Arrange
        var email = new SendEmailDto
        {
            To = "test@example.com",
            Subject = "Test",
            Body = "Test body"
        };

        // Act
        var item = new EmailQueueItem(email, RetryCount: 2);

        // Assert
        item.RetryCount.Should().Be(2);
    }

    [Fact]
    public void EmailSendResult_Success_HasCorrectProperties()
    {
        // Act
        var result = EmailSendResult.Success("msg-123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("msg-123");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void EmailSendResult_Failure_HasCorrectProperties()
    {
        // Act
        var result = EmailSendResult.Failure("Connection refused");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Connection refused");
        result.MessageId.Should().BeNull();
    }

    #endregion

    #region Template Model Tests

    [Fact]
    public void VerificationEmailModel_CreatesWithDefaults()
    {
        // Act
        var model = new VerificationEmailModel
        {
            UserName = "John Doe",
            VerificationUrl = "https://example.com/verify"
        };

        // Assert
        model.UserName.Should().Be("John Doe");
        model.VerificationUrl.Should().Be("https://example.com/verify");
        model.ExpirationHours.Should().Be(24);
        model.AppName.Should().Be("Quater Water Quality");
        model.Year.Should().Be(DateTime.UtcNow.Year);
    }

    [Fact]
    public void PasswordResetEmailModel_CreatesWithDefaults()
    {
        // Act
        var model = new PasswordResetEmailModel
        {
            UserName = "Jane Doe",
            ResetUrl = "https://example.com/reset"
        };

        // Assert
        model.UserName.Should().Be("Jane Doe");
        model.ResetUrl.Should().Be("https://example.com/reset");
        model.ExpirationMinutes.Should().Be(60);
    }

    [Fact]
    public void WelcomeEmailModel_HasDefaultFeatures()
    {
        // Act
        var model = new WelcomeEmailModel
        {
            UserName = "New User",
            LoginUrl = "https://example.com/login"
        };

        // Assert
        model.Features.Should().HaveCount(4);
        model.Features.Should().Contain("Track and analyze water quality samples");
    }

    [Fact]
    public void SecurityAlertEmailModel_SetsTimestamp()
    {
        // Act
        var before = DateTimeOffset.UtcNow;
        var model = new SecurityAlertEmailModel
        {
            UserName = "Secure User",
            AlertType = "New Login",
            AlertMessage = "Login from new device"
        };
        var after = DateTimeOffset.UtcNow;

        // Assert
        model.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    #endregion

    #region Scriban Template Service Tests

    [Fact]
    public async Task ScribanEmailTemplateService_RendersVerificationTemplate()
    {
        // Arrange
        var service = new ScribanEmailTemplateService(_templateLoggerMock.Object);
        var model = new VerificationEmailModel
        {
            UserName = "John Doe",
            VerificationUrl = "https://example.com/verify?token=abc123"
        };

        // Act
        var html = await service.RenderAsync("verification", model);

        // Assert
        html.Should().Contain("John Doe");
        html.Should().Contain("https://example.com/verify?token=abc123");
        html.Should().Contain("Verify Your Email Address");
        html.Should().Contain("<!DOCTYPE html>");
    }

    [Fact]
    public async Task ScribanEmailTemplateService_RendersPasswordResetTemplate()
    {
        // Arrange
        var service = new ScribanEmailTemplateService(_templateLoggerMock.Object);
        var model = new PasswordResetEmailModel
        {
            UserName = "Jane Doe",
            ResetUrl = "https://example.com/reset?token=xyz789"
        };

        // Act
        var html = await service.RenderAsync("password-reset", model);

        // Assert
        html.Should().Contain("Jane Doe");
        html.Should().Contain("https://example.com/reset?token=xyz789");
        html.Should().Contain("Reset Your Password");
    }

    [Fact]
    public async Task ScribanEmailTemplateService_RendersWelcomeTemplate()
    {
        // Arrange
        var service = new ScribanEmailTemplateService(_templateLoggerMock.Object);
        var model = new WelcomeEmailModel
        {
            UserName = "New User",
            LoginUrl = "https://example.com/login"
        };

        // Act
        var html = await service.RenderAsync("welcome", model);

        // Assert
        html.Should().Contain("New User");
        html.Should().Contain("https://example.com/login");
        html.Should().Contain("Welcome to Quater Water Quality!");
    }

    [Fact]
    public async Task ScribanEmailTemplateService_RendersSecurityAlertTemplate()
    {
        // Arrange
        var service = new ScribanEmailTemplateService(_templateLoggerMock.Object);
        var model = new SecurityAlertEmailModel
        {
            UserName = "Secure User",
            AlertType = "New Login Detected",
            AlertMessage = "A new login was detected from an unknown device."
        };

        // Act
        var html = await service.RenderAsync("security-alert", model);

        // Assert
        html.Should().Contain("Secure User");
        html.Should().Contain("New Login Detected");
        html.Should().Contain("A new login was detected from an unknown device.");
        html.Should().Contain("Security Alert");
    }

    [Fact]
    public async Task ScribanEmailTemplateService_ThrowsOnInvalidTemplateName()
    {
        // Arrange
        var service = new ScribanEmailTemplateService(_templateLoggerMock.Object);
        var model = new VerificationEmailModel
        {
            UserName = "Test",
            VerificationUrl = "https://test.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RenderAsync("nonexistent-template", model));
    }

    #endregion

    #region SmtpEmailService Tests

    [Fact]
    public void SmtpEmailService_Constructor_ThrowsOnNullConfiguration()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SmtpEmailService(null!, _loggerMock.Object, _templateServiceMock.Object));
    }

    [Fact]
    public void SmtpEmailService_Constructor_ThrowsOnNullLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SmtpEmailService(_configuration, null!, _templateServiceMock.Object));
    }

    [Fact]
    public void SmtpEmailService_Constructor_ThrowsOnNullTemplateService()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SmtpEmailService(_configuration, _loggerMock.Object, null!));
    }

    [Fact]
    public async Task SmtpEmailService_SendAsync_ThrowsOnNullEmail()
    {
        // Arrange
        var service = new SmtpEmailService(_configuration, _loggerMock.Object, _templateServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendAsync(null!));
    }

    #endregion
}
