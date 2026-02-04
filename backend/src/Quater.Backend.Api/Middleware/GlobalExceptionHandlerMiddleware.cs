using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Exceptions;

namespace Quater.Backend.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions,
/// logs them, and returns consistent error responses.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        // Determine status code and error message based on exception type
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                validationEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToList()
            ),
            NotFoundException => (
                HttpStatusCode.NotFound,
                exception.Message,
                null
            ),
            BadRequestException => (
                HttpStatusCode.BadRequest,
                exception.Message,
                null
            ),
            ConflictException => (
                HttpStatusCode.Conflict,
                exception.Message,
                null
            ),
            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                "The record was modified by another user. Please refresh and try again.",
                null
            ),
            ForbiddenException => (
                HttpStatusCode.Forbidden,
                exception.Message,
                null
            ),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "The requested resource was not found",
                null
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized access",
                null
            ),
            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                exception.Message,
                null
            ),
            ArgumentException => (
                HttpStatusCode.BadRequest,
                exception.Message,
                null
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An internal server error occurred",
                null
            )
        };

        // Create error response
        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Errors = errors,
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        // Add stack trace in development mode
        if (_environment.IsDevelopment())
        {
            response.StackTrace = exception.StackTrace;
            response.InnerException = exception.InnerException?.Message;
        }

        // Set response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

/// <summary>
/// Standard error response model.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Validation errors (if applicable).
    /// </summary>
    public object? Errors { get; set; }

    /// <summary>
    /// Request trace identifier for debugging.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Stack trace (only in development mode).
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Inner exception message (only in development mode).
    /// </summary>
    public string? InnerException { get; set; }
}

/// <summary>
/// Extension methods for registering the global exception handler middleware.
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    /// <summary>
    /// Adds the global exception handler middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
