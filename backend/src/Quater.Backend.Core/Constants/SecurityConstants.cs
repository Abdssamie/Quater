namespace Quater.Backend.Core.Constants;

/// <summary>
/// Contains security-related constants for authentication and authorization.
/// </summary>
public static class SecurityConstants
{
    /// <summary>
    /// The minimum delay in milliseconds applied to authentication endpoints
    /// to prevent timing-based user enumeration attacks.
    /// 
    /// This delay ensures that response times are consistent regardless of
    /// whether a user exists in the system, preventing attackers from
    /// determining valid email addresses through timing analysis.
    /// 
    /// Value: 200ms - Industry standard for timing attack protection
    /// </summary>
    public const int TimingProtectionDelayMs = 200;
}
