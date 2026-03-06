namespace Quater.Desktop.Core.Auth.Services;

public interface IPermissionService
{
    bool CanViewAuditWorkflow();
    bool CanViewSyncCenter();
    bool CanCreateSample();
    bool CanEditSample();
    bool CanDeleteSample();
    bool CanCreateTestResult();
    bool CanEditTestResult();
    bool CanDeleteTestResult();
}
