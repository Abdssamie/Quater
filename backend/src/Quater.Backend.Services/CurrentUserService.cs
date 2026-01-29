using Microsoft.AspNetCore.Http;
using Quater.Backend.Data.Interceptors;
using System.Security.Claims;

namespace Quater.Backend.Services;

/// <summary>
/// Service for retrieving current user information from HTTP context.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current authenticated user's ID.
    /// </summary>
    /// <returns>The user ID from claims, or "System" if not authenticated.</returns>
    public string GetCurrentUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId ?? "System";
    }
}
