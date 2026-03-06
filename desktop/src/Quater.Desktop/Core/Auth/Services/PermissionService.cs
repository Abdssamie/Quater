using Quater.Desktop.Api.Model;

namespace Quater.Desktop.Core.Auth.Services;

public sealed class PermissionService : IPermissionService
{
    public bool CanAccessAuditWorkflow(UserLabDto? selectedLab)
    {
        return selectedLab is not null && selectedLab.Role == UserRole.NUMBER_3;
    }
}
