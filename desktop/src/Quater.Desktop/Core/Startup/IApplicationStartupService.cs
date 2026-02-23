namespace Quater.Desktop.Core.Startup;

public interface IApplicationStartupService
{
    Task<StartupResult> InitializeAsync(CancellationToken ct = default);
}

public sealed class StartupResult
{
    public bool IsSuccess { get; init; }
    public bool RequiresOnboarding { get; init; }
    public string? ErrorMessage { get; init; }

    public static StartupResult Success() => new() { IsSuccess = true };
    public static StartupResult NeedsOnboarding() => new() { IsSuccess = true, RequiresOnboarding = true };
    public static StartupResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
