using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Helpers;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data;
using Quater.Backend.Infrastructure.Email;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Services;

public sealed class UserInvitationService(
    QuaterDbContext context,
    UserManager<User> userManager,
    IUserLabService userLabService,
    IEmailQueue emailQueue,
    IEmailTemplateService emailTemplateService,
    IOptions<EmailSettings> emailSettings,
    ILogger<UserInvitationService> logger,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider) : IUserInvitationService
{
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    public async Task<UserInvitationDto> InviteUserAsync(CreateUserInvitationDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userExists = await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == dto.Email, ct);

        if (userExists)
            throw new ConflictException(ErrorMessages.UserAlreadyExists);

        var user = new User
        {
            Email = dto.Email,
            UserName = dto.UserName,
            IsActive = false,
            EmailConfirmed = false
        };

        var token = GenerateToken();
        var tokenHash = HashToken(token);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var currentUserId = currentUserService.GetCurrentUserId();
        var adminLabIds = await GetAdminLabIdsAsync(currentUserId, ct);
        EnsureAdminForLabs(adminLabIds, dto.LabAssignments.Select(assignment => assignment.LabId));

        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new BadRequestException($"{ErrorMessages.UserCreationFailed}: {errors}");
        }

        var invitation = new UserInvitation
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = dto.Email,
            TokenHash = tokenHash,
            ExpiresAt = now.AddDays(AppConstants.Invitations.ExpirationDays),
            Status = InvitationStatus.Pending,
            InvitedByUserId = currentUserId,
            CreatedAt = now,
            CreatedBy = currentUserId
        };

        context.UserInvitations.Add(invitation);
        await context.SaveChangesAsync(ct);

        foreach (var assignment in dto.LabAssignments)
        {
            await userLabService.AddUserToLabAsync(user.Id, assignment.LabId, assignment.Role, ct);
        }

        await transaction.CommitAsync(ct);

        var invitedByUser = await userManager.FindByIdAsync(currentUserId.ToString());
        var invitedByName = string.IsNullOrWhiteSpace(invitedByUser?.UserName)
            ? invitedByUser?.Email
            : invitedByUser.UserName;
        invitedByName = string.IsNullOrWhiteSpace(invitedByName)
            ? "System Admin"
            : invitedByName;

        await SendInvitationEmailAsync(user, token, invitedByName, ct);

        var createdInvitation = await context.UserInvitations
            .AsNoTracking()
            .Include(i => i.User)
            .ThenInclude(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .Include(i => i.InvitedBy)
            .FirstAsync(i => i.Id == invitation.Id, ct);

        return MapToDto(createdInvitation);
    }

    public async Task<UserInvitationDto> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var tokenHash = HashToken(token);

        var invitation = await context.UserInvitations
            .AsNoTracking()
            .Include(i => i.User)
            .ThenInclude(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .Include(i => i.InvitedBy)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

        if (invitation == null)
            throw new NotFoundException(ErrorMessages.InvitationNotFound);

        if (invitation.Status != InvitationStatus.Pending)
        {
            var errorMessage = invitation.Status switch
            {
                InvitationStatus.Accepted => ErrorMessages.InvitationAlreadyAccepted,
                InvitationStatus.Revoked => ErrorMessages.InvitationRevoked,
                InvitationStatus.Expired => ErrorMessages.InvitationExpired,
                _ => ErrorMessages.InvitationAlreadyAccepted
            };

            throw new BadRequestException(errorMessage);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (invitation.ExpiresAt < now)
            throw new BadRequestException(ErrorMessages.InvitationExpired);

        return MapToDto(invitation);
    }

    public async Task<UserInvitationDto> AcceptInvitationAsync(AcceptInvitationDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var tokenHash = HashToken(dto.Token);

        var invitation = await context.UserInvitations
            .Include(i => i.User)
            .ThenInclude(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .Include(i => i.InvitedBy)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

        if (invitation == null)
            throw new NotFoundException(ErrorMessages.InvitationNotFound);

        if (invitation.Status != InvitationStatus.Pending)
        {
            var errorMessage = invitation.Status switch
            {
                InvitationStatus.Accepted => ErrorMessages.InvitationAlreadyAccepted,
                InvitationStatus.Revoked => ErrorMessages.InvitationRevoked,
                InvitationStatus.Expired => ErrorMessages.InvitationExpired,
                _ => ErrorMessages.InvitationAlreadyAccepted
            };

            throw new BadRequestException(errorMessage);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (invitation.ExpiresAt < now)
            throw new BadRequestException(ErrorMessages.InvitationExpired);

        var passwordResult = await userManager.AddPasswordAsync(invitation.User, dto.Password);
        if (!passwordResult.Succeeded)
        {
            var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
            throw new BadRequestException($"{ErrorMessages.UserUpdateFailed}: {errors}");
        }

        invitation.User.EmailConfirmed = true;
        invitation.User.IsActive = true;

        var userUpdateResult = await userManager.UpdateAsync(invitation.User);
        if (!userUpdateResult.Succeeded)
        {
            var errors = string.Join(", ", userUpdateResult.Errors.Select(e => e.Description));
            throw new BadRequestException($"{ErrorMessages.UserUpdateFailed}: {errors}");
        }

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = now;
        invitation.UpdatedAt = now;
        invitation.UpdatedBy = invitation.UserId;

        await context.SaveChangesAsync(ct);

        await SendWelcomeEmailAsync(invitation.User, ct);

        return MapToDto(invitation);
    }

    public async Task RevokeInvitationAsync(Guid invitationId, CancellationToken ct = default)
    {
        var invitation = await context.UserInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, ct);

        if (invitation == null)
            throw new NotFoundException(ErrorMessages.InvitationNotFound);

        if (invitation.Status != InvitationStatus.Pending)
            throw new BadRequestException(ErrorMessages.InvitationAlreadyAccepted);

        var currentUserId = currentUserService.GetCurrentUserId();
        var adminLabIds = await GetAdminLabIdsAsync(currentUserId, ct);
        var invitedUserLabIds = await context.UserLabs
            .AsNoTracking()
            .Where(ul => ul.UserId == invitation.UserId)
            .Select(ul => ul.LabId)
            .ToListAsync(ct);
        EnsureAdminForLabs(adminLabIds, invitedUserLabIds);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        invitation.Status = InvitationStatus.Revoked;
        invitation.UpdatedAt = now;
        invitation.UpdatedBy = currentUserId;

        await context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<UserInvitationDto>> GetPendingInvitationsAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, PaginationHelper.MaxPageSize);
        var currentUserId = currentUserService.GetCurrentUserId();
        var adminLabIds = await GetAdminLabIdsAsync(currentUserId, ct);

        var query = context.UserInvitations
            .AsNoTracking()
            .Include(i => i.User)
            .ThenInclude(u => u.UserLabs)
            .ThenInclude(ul => ul.Lab)
            .Include(i => i.InvitedBy)
            .Where(i => i.Status == InvitationStatus.Pending)
            .Where(i => i.User != null
                && i.User.UserLabs.Any()
                && i.User.UserLabs.All(ul => adminLabIds.Contains(ul.LabId)))
            .OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Paginate(normalizedPage, normalizedPageSize)
            .ToListAsync(ct);

        return new PagedResult<UserInvitationDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = normalizedPage,
            PageSize = normalizedPageSize
        };
    }

    public async Task ExpireOldInvitationsAsync(CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var currentUserId = SystemUser.GetId();

        var expiredInvitations = await context.UserInvitations
            .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAt < now)
            .ToListAsync(ct);

        if (expiredInvitations.Count == 0)
            return;

        foreach (var invitation in expiredInvitations)
        {
            invitation.Status = InvitationStatus.Expired;
            invitation.UpdatedAt = now;
            invitation.UpdatedBy = currentUserId;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "{Method} expired {Count} invitations",
            nameof(ExpireOldInvitationsAsync),
            expiredInvitations.Count);
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes);
        return token.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    private async Task<List<Guid>> GetAdminLabIdsAsync(Guid userId, CancellationToken ct)
    {
        return await context.UserLabs
            .AsNoTracking()
            .Where(ul => ul.UserId == userId && ul.Role == UserRole.Admin)
            .Select(ul => ul.LabId)
            .Distinct()
            .ToListAsync(ct);
    }

    private static void EnsureAdminForLabs(IEnumerable<Guid> adminLabIds, IEnumerable<Guid> labIds)
    {
        var adminLabIdSet = adminLabIds.ToHashSet();
        var hasMissingLab = labIds.Any(labId => !adminLabIdSet.Contains(labId));

        if (hasMissingLab)
            throw new ForbiddenException(ErrorMessages.InsufficientLabPermissions);
    }

    private async Task SendInvitationEmailAsync(
        User user,
        string token,
        string invitedByName,
        CancellationToken ct)
    {
        var frontendUrl = _emailSettings.FrontendUrl;
        var encodedToken = WebUtility.UrlEncode(token);
        var inviteUrl = $"{frontendUrl}/accept-invitation?token={encodedToken}";

        var htmlBody = await emailTemplateService.RenderAsync("invitation", new InvitationEmailModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            InvitationUrl = inviteUrl,
            ExpirationDays = AppConstants.Invitations.ExpirationDays,
            InvitedByName = invitedByName
        });

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = "You're invited to Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await emailQueue.QueueAsync(new EmailQueueItem(emailDto), ct);
    }

    private async Task SendWelcomeEmailAsync(User user, CancellationToken ct)
    {
        var frontendUrl = _emailSettings.FrontendUrl;

        var model = new WelcomeEmailModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            LoginUrl = $"{frontendUrl}/login"
        };

        var htmlBody = await emailTemplateService.RenderAsync("welcome", model);

        var emailDto = new SendEmailDto
        {
            To = user.Email!,
            Subject = "Welcome to Quater Water Quality",
            Body = htmlBody,
            IsHtml = true
        };

        await emailQueue.QueueAsync(new EmailQueueItem(emailDto), ct);
    }

    private static UserInvitationDto MapToDto(UserInvitation invitation)
    {
        var assignedLabs = invitation.User?.UserLabs?.Select(ul => new UserLabDto
        {
            LabId = ul.LabId,
            LabName = ul.Lab?.Name ?? string.Empty,
            Role = ul.Role,
            AssignedAt = ul.AssignedAt
        }).ToList() ?? [];

        var invitedByUserName = invitation.InvitedBy?.UserName
            ?? invitation.InvitedBy?.Email
            ?? string.Empty;

        return new UserInvitationDto(
            invitation.Id,
            invitation.UserId,
            invitation.Email,
            invitation.Status,
            invitation.ExpiresAt,
            invitation.InvitedByUserId,
            invitedByUserName,
            invitation.AcceptedAt,
            invitation.CreatedAt)
        {
            AssignedLabs = assignedLabs
        };
    }
}
