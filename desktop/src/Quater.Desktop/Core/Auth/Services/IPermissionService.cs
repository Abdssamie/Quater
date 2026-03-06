using Quater.Desktop.Api.Model;

namespace Quater.Desktop.Core.Auth.Services;

public interface IPermissionService
{
    bool CanAccessAuditWorkflow(UserLabDto? selectedLab);
}
