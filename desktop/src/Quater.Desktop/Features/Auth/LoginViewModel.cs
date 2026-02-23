using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Quater.Desktop.Core;
using Quater.Desktop.Core.Auth.Services;
using Quater.Desktop.Core.State;

namespace Quater.Desktop.Features.Auth;

public sealed partial class LoginViewModel : ViewModelBase
{
   private readonly IAuthService _authService;
   private readonly AuthSessionManager _authSessionManager;
   private readonly ILogger<LoginViewModel> _logger;
   private CancellationTokenSource? _loginCts;

   [ObservableProperty]
   private bool _isLoading;

   [ObservableProperty]
   private string _errorMessage = string.Empty;

    public LoginViewModel(
       IAuthService authService,
       AppState appState,
       AuthSessionManager authSessionManager,
       ILogger<LoginViewModel> logger)
    {
       _authService = authService;
       _authSessionManager = authSessionManager;
       _logger = logger;

      if (!string.IsNullOrWhiteSpace(appState.AuthNotice))
      {
         ErrorMessage = appState.AuthNotice;
         appState.AuthNotice = string.Empty;
      }
   }

   [RelayCommand]
   private async Task SignIn()
   {
      try
      {
         IsLoading = true;
         ErrorMessage = string.Empty;

         _loginCts?.Cancel();
         _loginCts?.Dispose();
         _loginCts = new CancellationTokenSource();

         _logger.LogInformation("Starting login flow");
         var result = await _authService.LoginAsync(_loginCts.Token);
         if (result.IsError)
         {
            ErrorMessage = result.Error ?? "Authentication failed.";
            _logger.LogWarning("Login failed: {Error}", ErrorMessage);
            return;
         }

         await _authSessionManager.HandleLoginSuccessAsync(result);
      }
      catch (OperationCanceledException)
      {
         ErrorMessage = "Sign-in canceled. Please try again.";
         _logger.LogWarning("Login canceled by user");
      }
      catch (Exception ex)
      {
         ErrorMessage = ex.Message;
         _logger.LogError(ex, "Login failed with exception");
      }
      finally
      {
         _loginCts?.Dispose();
         _loginCts = null;
         IsLoading = false;
      }
   }

   [RelayCommand]
   private void CancelSignIn()
   {
      if (!IsLoading)
      {
         return;
      }

      _loginCts?.Cancel();
   }
}
