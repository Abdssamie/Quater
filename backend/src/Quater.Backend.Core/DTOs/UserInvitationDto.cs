using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;

namespace Quater.Backend.Core.DTOs;

public sealed record UserInvitationDto(
    Guid Id,
    Guid UserId,
    string Email,
    InvitationStatus Status,
    DateTime ExpiresAt,
    Guid InvitedByUserId,
    string InvitedByUserName,
    DateTime? AcceptedAt,
    DateTime CreatedAt)
{
    public List<UserLabDto> AssignedLabs { get; init; } = [];
}

public sealed record CreateUserInvitationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<InvitationLabAssignmentDto> LabAssignments { get; init; } = [];
}

public sealed record InvitationLabAssignmentDto
{
    [Required]
    public Guid LabId { get; init; }

    [Required]
    public UserRole Role { get; init; }
}

public sealed record AcceptInvitationDto
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;
}
