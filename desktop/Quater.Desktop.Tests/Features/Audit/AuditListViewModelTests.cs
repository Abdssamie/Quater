using Moq;
using Quater.Desktop.Api.Api;
using Quater.Desktop.Api.Model;
using Quater.Desktop.Core.Api;
using Quater.Desktop.Core.Dialogs;
using Quater.Desktop.Core.Export;
using Quater.Desktop.Features.Audit.List;

namespace Quater.Desktop.Tests.Features.Audit;

public sealed class AuditListViewModelTests
{
    [Fact]
    public async Task LoadAuditLogsCommand_MapsFiltersToAuditFilterDto()
    {
        var auditApi = new Mock<IAuditLogsApi>(MockBehavior.Strict);
        var apiFactory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        var csvExportService = new Mock<ICsvExportService>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        var startDate = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 02, 28, 0, 0, 0, DateTimeKind.Utc);

        apiFactory.Setup(factory => factory.GetAuditLogsApi()).Returns(auditApi.Object);
        auditApi.Setup(api => api.ApiAuditLogsFilterPostAsync(
                It.IsAny<string?>(),
                It.Is<AuditLogFilterDto>(dto =>
                    dto.EntityType == EntityType.NUMBER_2 &&
                    dto.Action == AuditAction.NUMBER_3 &&
                    dto.StartDate == startDate &&
                    dto.EndDate == endDate &&
                    dto.UserId == userId &&
                    dto.PageNumber == 3 &&
                    dto.PageSize == 75),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuditLogDtoPagedResult(items: [], totalCount: 0, pageNumber: 3, pageSize: 75));

        var viewModel = new AuditListViewModel(apiFactory.Object, dialogService.Object, csvExportService.Object)
        {
            EntityTypeFilter = EntityType.NUMBER_2,
            ActionFilter = AuditAction.NUMBER_3,
            StartDateFilter = startDate,
            EndDateFilter = endDate,
            UserIdFilter = userId,
            PageNumber = 3,
            PageSize = 75
        };

        await viewModel.LoadAuditLogsCommand.ExecuteAsync(null);

        auditApi.VerifyAll();
        apiFactory.VerifyAll();
    }

    [Fact]
    public async Task ExportCsvCommand_UsesCurrentlyFilteredRows()
    {
        var auditApi = new Mock<IAuditLogsApi>(MockBehavior.Strict);
        var apiFactory = new Mock<IApiClientFactory>(MockBehavior.Strict);
        var dialogService = new Mock<IDialogService>(MockBehavior.Strict);
        var csvExportService = new Mock<ICsvExportService>(MockBehavior.Strict);

        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var first = new AuditLogDto(id: firstId, userEmail: "first@quater.app", timestamp: new DateTime(2026, 02, 10, 10, 30, 0, DateTimeKind.Utc));
        var second = new AuditLogDto(id: secondId, userEmail: "second@quater.app", timestamp: new DateTime(2026, 02, 11, 11, 30, 0, DateTimeKind.Utc));

        apiFactory.Setup(factory => factory.GetAuditLogsApi()).Returns(auditApi.Object);
        auditApi.Setup(api => api.ApiAuditLogsFilterPostAsync(
                It.IsAny<string?>(),
                It.IsAny<AuditLogFilterDto>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuditLogDtoPagedResult(items: [first, second], totalCount: 2, pageNumber: 1, pageSize: 50));

        csvExportService.Setup(service => service.ExportAuditLogs(
                It.Is<IReadOnlyList<AuditLogDto>>(rows =>
                    rows.Count == 2 &&
                    rows[0].Id == firstId &&
                    rows[1].Id == secondId)))
            .Returns("id,userEmail\n...");

        dialogService.Setup(service => service.ShowSuccess("Audit logs exported to CSV."));

        var viewModel = new AuditListViewModel(apiFactory.Object, dialogService.Object, csvExportService.Object);

        await viewModel.LoadAuditLogsCommand.ExecuteAsync(null);
        await viewModel.ExportCsvCommand.ExecuteAsync(null);

        csvExportService.VerifyAll();
        dialogService.VerifyAll();
    }
}
