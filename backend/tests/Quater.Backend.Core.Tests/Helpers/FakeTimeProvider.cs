namespace Quater.Backend.Core.Tests.Helpers;

/// <summary>
/// Fake time provider for testing time-dependent code
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    public DateTimeOffset CurrentTime { get; }

    public FakeTimeProvider()
    {
        CurrentTime = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }
}
