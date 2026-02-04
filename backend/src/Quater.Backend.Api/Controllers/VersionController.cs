using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Quater.Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Get version from assembly (set by Directory.Build.props)
        var assemblyVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString();

        // Prefer environment variable (for Docker builds), fallback to assembly version
        var envVersion = Environment.GetEnvironmentVariable("APP_VERSION");

        if (string.IsNullOrWhiteSpace(envVersion) && string.IsNullOrWhiteSpace(assemblyVersion))
        {
            throw new InvalidOperationException(
                "Version information not found. Ensure APP_VERSION environment variable is set or assembly version is configured in Directory.Build.props");
        }

        var version = envVersion ?? assemblyVersion!;
        var buildDate = Environment.GetEnvironmentVariable("BUILD_DATE") ?? "unknown";

        return Ok(new
        {
            version,
            apiVersion = "v1",
            buildDate,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"
        });
    }
}
