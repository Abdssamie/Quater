using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Core.Auth.Services;

public sealed class PermissionService(AppState appState) : IPermissionService
{
    public bool CanViewAuditWorkflow()
    {
        return HasRoleAtLeast(UserRole.NUMBER_2);
    }

    public bool CanViewSyncCenter()
    {
        return HasRoleAtLeast(UserRole.NUMBER_2);
    }

    public bool CanCreateSample()
    {
        return HasRoleAtLeast(UserRole.NUMBER_2);
    }

    public bool CanEditSample()
    {
        return HasRoleAtLeast(UserRole.NUMBER_2);
    }

    public bool CanDeleteSample()
    {
        return HasRoleAtLeast(UserRole.NUMBER_2);
    }

    public bool CanCreateTestResult()
    {
        return HasRoleAtLeast(UserRole.NUMBER_2);
    }

    public bool CanEditTestResult()
    {
        return HasRoleAtLeast(UserRole.NUMBER_2);
    }

    public bool CanDeleteTestResult()
    {
        return HasRoleAtLeast(UserRole.NUMBER_2);
    }

    private bool HasRoleAtLeast(UserRole minimumRole)
    {
        if (appState.CurrentLabId == Guid.Empty)
        {
            return false;
        }

        var currentLab = appState.AvailableLabs.FirstOrDefault(lab => lab.LabId == appState.CurrentLabId);
        if (currentLab is null)
        {
            return false;
        }

        return currentLab.Role >= minimumRole;
    }

    public bool CanAccessAuditWorkflow(UserLabDto? selectedLab)
    {
        return selectedLab is not null && selectedLab.Role >= UserRole.NUMBER_2;
    }
}
