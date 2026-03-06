using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Tests.Core.Auth;

public sealed class PermissionServiceTests
{
    [Fact]
    public void CanViewAuditWorkflow_WhenCurrentLabRoleIsAtLeastTechnician_ReturnsTrue()
    {
        var appState = CreateAppStateWithSelectedLab(UserRole.NUMBER_2);
        var service = new PermissionService(appState);

        var canView = service.CanViewAuditWorkflow();

        Assert.True(canView);
    }

    [Fact]
    public void CanViewSyncCenter_WhenCurrentLabRoleIsViewer_ReturnsFalse()
    {
        var appState = CreateAppStateWithSelectedLab(UserRole.NUMBER_1);
        var service = new PermissionService(appState);

        var canView = service.CanViewSyncCenter();

        Assert.False(canView);
    }

    [Fact]
    public void SampleActions_WhenCurrentLabRoleIsTechnician_AllowsCreateEditDelete()
    {
        var appState = CreateAppStateWithSelectedLab(UserRole.NUMBER_2);
        var service = new PermissionService(appState);

        Assert.True(service.CanCreateSample());
        Assert.True(service.CanEditSample());
        Assert.True(service.CanDeleteSample());
    }

    [Fact]
    public void TestResultActions_WhenCurrentLabRoleIsViewer_DisallowsCreateEditDelete()
    {
        var appState = CreateAppStateWithSelectedLab(UserRole.NUMBER_1);
        var service = new PermissionService(appState);

        Assert.False(service.CanCreateTestResult());
        Assert.False(service.CanEditTestResult());
        Assert.False(service.CanDeleteTestResult());
    }

    private static AppState CreateAppStateWithSelectedLab(UserRole role)
    {
        var labId = Guid.NewGuid();
        return new AppState
        {
            CurrentLabId = labId,
            AvailableLabs = [new UserLabDto(labId: labId, labName: "Lab", role: role)]
        };
    }
}
