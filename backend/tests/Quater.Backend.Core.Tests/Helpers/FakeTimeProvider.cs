namespace Quater.Backend.Core.Tests.Helpers;

/// <summary>
/// Fake time provider for testing time-dependent code
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _currentTime;

    public FakeTimeProvider()
    {
        _currentTime = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    public FakeTimeProvider(DateTimeOffset startTime)
    {
        _currentTime = startTime;
    }

    public override DateTimeOffset GetUtcNow() => _currentTime;

    public void SetUtcNow(DateTimeOffset time)
    {
        _currentTime = time;
    }

    public void Advance(TimeSpan timeSpan)
    {
        _currentTime = _currentTime.Add(timeSpan);
    }

    public void AdvanceDays(int days)
    {
        _currentTime = _currentTime.AddDays(days);
    }

    public void AdvanceHours(int hours)
    {
        _currentTime = _currentTime.AddHours(hours);
    }

    public void Reset()
    {
        _currentTime = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }
}
