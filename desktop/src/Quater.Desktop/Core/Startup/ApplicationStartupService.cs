using Microsoft.Extensions.Logging;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.Navigation;
using Quater.Desktop.Core.Settings;
using Serilog;

namespace Quater.Desktop.Core.Startup;

public sealed class ApplicationStartupService(
    ISettingsStore settingsStore,
    AppSettings appSettings,
    AuthSessionManager authSessionManager,
    ILogger<ApplicationStartupService> logger,
    INavigationService navigationService) : IApplicationStartupService
{
   public async Task<StartupResult> InitializeAsync(
      CancellationToken ct = default)
   {
       try
       {
           // 1. Load settings
           var settings = await settingsStore.LoadAsync(ct);
           appSettings.BackendUrl = settings.BackendUrl;
           appSettings.IsOnboarded = settings.IsOnboarded;
            logger.LogInformation("Initialized settings");

            // Update API client configuration with loaded backend URL
            var backendUrl = settings.BackendUrl;
            if (!string.IsNullOrWhiteSpace(backendUrl))
            {
                var config = new Quater.Desktop.Api.Client.Configuration
                {
                    BasePath = backendUrl
                };
                Quater.Desktop.Api.Client.GlobalConfiguration.Instance = config;
            }

          // 2. Check onboarding status
         if (!settings.IsOnboarded || string.IsNullOrWhiteSpace(settings.BackendUrl))
         {
            return StartupResult.NeedsOnboarding();
         }

         logger.LogInformation("Needs onboarding");
         // 3. Register navigation routes
         RegisterNavigationRoutes();
         logger.LogInformation("Registered navigation routes");

         // 4. Configure API unauthorized handler
         ConfigureApiUnauthorizedHandler();
         logger.LogInformation("Configured api auth hadler");

         // 5. Initialize auth session
         await authSessionManager.InitializeAsync(ct);
         logger.LogInformation("Initialized auth session manager");

         return StartupResult.Success();
      }
      catch (Exception ex)
      {
         Log.Error(ex, "Application startup failed");
         return StartupResult.Failure($"Startup failed: {ex.Message}");
      }
   }

   private void RegisterNavigationRoutes()
   {
      navigationService.RegisterRoute<Features.Dashboard.DashboardViewModel>(new(
          "Dashboard",
          "M13,3V9H21V3M13,21H21V11H13M3,21H11V15H3M3,13H11V3H3V13Z",
          typeof(Features.Dashboard.DashboardViewModel)
      ));

      navigationService.RegisterRoute<Features.Samples.List.SampleListViewModel>(new(
          "Samples",
          "M18,17L21,22H3L6,17H18M18,17L14,5H10L6,17H18M15,4H9L8,4H9L12,1L15,4Z",
          typeof(Features.Samples.List.SampleListViewModel),
          1
      ));

      navigationService.RegisterRoute<Features.TestResults.List.TestResultListViewModel>(new(
          "Test Results",
          "M9,3V4H4V6H5V19A2,2 0 0,0 7,21H17A2,2 0 0,0 19,19V6H20V4H15V3H9M7,6H17V19H7V6M9,8V10H15V8H9M9,12V14H15V12H9M9,16V18H13V16H9Z",
          typeof(Features.TestResults.List.TestResultListViewModel),
          2
      ));

      navigationService.RegisterRoute<Features.Audit.List.AuditListViewModel>(new(
          "Audit",
          "M3,3H21V7H3V3M5,9H19V11H5V9M5,13H19V15H5V13M5,17H19V19H5V17Z",
          typeof(Features.Audit.List.AuditListViewModel),
          3
      ));

      navigationService.RegisterRoute<Features.Sync.Center.SyncCenterViewModel>(new(
          "Sync Center",
          "M12,3C16.42,3 20,4.79 20,7C20,9.21 16.42,11 12,11C7.58,11 4,9.21 4,7C4,4.79 7.58,3 12,3M4,9V12C4,14.21 7.58,16 12,16C16.42,16 20,14.21 20,12V9C20,11.21 16.42,13 12,13C7.58,13 4,11.21 4,9M4,14V17C4,19.21 7.58,21 12,21C16.42,21 20,19.21 20,17V14C20,16.21 16.42,18 12,18C7.58,18 4,16.21 4,14M16.5,8.25L15.16,6.91L12,10.07L10.67,8.74L9.33,10.08L12,12.75L16.5,8.25Z",
          typeof(Features.Sync.Center.SyncCenterViewModel),
          4
      ));

      navigationService.RegisterRoute<Features.Diagnostics.DiagnosticsViewModel>(new(
          "Diagnostics",
          "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M11,7V13H13V7H11M11,15V17H13V15H11Z",
          typeof(Features.Diagnostics.DiagnosticsViewModel),
          5
      ));
   }

   private void ConfigureApiUnauthorizedHandler()
   {
      Desktop.Api.Client.ApiClient.UnauthorizedResponseHandler = _ =>
          authSessionManager.HandleUnauthorizedAsync();
   }
}
