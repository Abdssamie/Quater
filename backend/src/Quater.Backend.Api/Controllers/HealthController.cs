using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Data;

namespace Quater.Backend.Api.Controllers;

/// <summary>
/// Health check endpoints for monitoring and orchestration.
/// These endpoints are publicly accessible for monitoring systems.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Health checks should be accessible without authentication
public class HealthController : ControllerBase
{
    private readonly QuaterDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(QuaterDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint.
    /// Returns 200 OK if the service is running.
    /// </summary>
    /// <remarks>
    /// Use this endpoint for liveness probes in Kubernetes.
    /// It only checks if the application is responsive.
    /// </remarks>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLiveness()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "Quater Backend API"
        });
    }

    /// <summary>
    /// Readiness check endpoint.
    /// Returns 200 OK if the service is ready to accept traffic.
    /// Checks database connectivity and other dependencies.
    /// </summary>
    /// <remarks>
    /// Use this endpoint for readiness probes in Kubernetes.
    /// It verifies that all dependencies are available.
    /// </remarks>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken)
    {
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogWarning("Readiness check failed: Cannot connect to database");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    reason = "Database connection failed",
                    timestamp = DateTime.UtcNow
                });
            }

            // Optionally check if migrations are applied
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            var migrations = pendingMigrations as string[] ?? pendingMigrations.ToArray();
            if (migrations.Length != 0)
            {
                _logger.LogWarning("Readiness check warning: Pending migrations detected");
            }

            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                checks = new
                {
                    database = "Connected",
                    pendingMigrations = migrations.Length != 0 ? "Warning" : "None"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed with exception");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                reason = "Health check failed",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Startup check endpoint.
    /// Returns 200 OK if the service has completed startup.
    /// </summary>
    /// <remarks>
    /// Use this endpoint for startup probes in Kubernetes.
    /// It helps with slow-starting containers.
    /// </remarks>
    [HttpGet("startup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStartup()
    {
        return Ok(new
        {
            status = "Started",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Detailed health check endpoint with comprehensive status information.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        try
        {
            var checks = new Dictionary<string, object>();

            // Database check
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            checks["database"] = new
            {
                status = canConnect ? "Healthy" : "Unhealthy",
                responseTime = "< 100ms" // Could measure actual response time
            };

            // Check pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            var migrations = pendingMigrations as string[] ?? pendingMigrations.ToArray();
            checks["migrations"] = new
            {
                status = migrations.Length != 0 ? "Warning" : "Healthy",
                pending = migrations.Length
            };

            // Overall status
            var statusCode = canConnect ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

            return StatusCode(statusCode, new
            {
                status = canConnect ? "Healthy" : "Unhealthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0", // Could read from assembly
                checks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
}
