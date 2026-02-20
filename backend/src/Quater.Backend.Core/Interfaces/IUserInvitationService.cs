using Quater.Backend.Core.DTOs;

namespace Quater.Backend.Core.Interfaces;

public interface IUserInvitationService
{
    Task<UserInvitationDto> InviteUserAsync(CreateUserInvitationDto dto, CancellationToken ct = default);
    Task<UserInvitationDto> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<UserInvitationDto> AcceptInvitationAsync(AcceptInvitationDto dto, CancellationToken ct = default);
    Task RevokeInvitationAsync(Guid invitationId, CancellationToken ct = default);
    Task<PagedResult<UserInvitationDto>> GetPendingInvitationsAsync(int page, int pageSize, CancellationToken ct = default);
    Task ExpireOldInvitationsAsync(CancellationToken ct = default);
}
