namespace Quater.Shared.Enums;

/// <summary>
/// Represents the status of a user invitation.
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    /// Invitation has been sent and is awaiting acceptance.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Invitation has been accepted by the user.
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// Invitation has expired and is no longer valid.
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Invitation has been revoked by an administrator.
    /// </summary>
    Revoked = 4
}
