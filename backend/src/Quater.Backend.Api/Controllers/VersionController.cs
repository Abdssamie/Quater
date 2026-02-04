using Microsoft.AspNetCore.Mvc;

namespace Quater.Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "unknown";
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
