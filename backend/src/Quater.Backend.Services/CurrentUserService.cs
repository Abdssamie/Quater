using Microsoft.AspNetCore.Http;
using Quater.Backend.Data.Interceptors;
using System.Security.Claims;
using Quater.Backend.Core.Constants;

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
    public Guid GetCurrentUserId()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return SystemUser.GetId();
        }

        return Guid.TryParse(userIdString, out var userId) ? userId :
            // Fallback to system user if parsing fails
            SystemUser.GetId();
    }
}
