using Rin.Core.Shared.Time;

namespace Rin.Core.Tests.Shared.Time;

public class ChronometerTests
{
    private ManualTimeProvider _clock = null!;
    private Chronometer _chronometer = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = new ManualTimeProvider();
        _chronometer = new Chronometer(_clock);
    }

    [Test]
    public void TheTotalAdvancesOnlyWhileRunning()
    {
        _clock.Advance(1.0);
        Assert.That(_chronometer.TotalSeconds, Is.Zero);

        _chronometer.Start();
        _clock.Advance(2.0);

        Assert.That(_chronometer.TotalSeconds, Is.EqualTo(2.0));
        Assert.That(_chronometer.IsRunning, Is.True);
    }

    [Test]
    public void SetTimeMovesTheTotalWhileRunning()
    {
        _chronometer.Start();
        _clock.Advance(3.0);

        _chronometer.SetTime(100.0);
        _clock.Advance(1.0);

        Assert.That(_chronometer.TotalSeconds, Is.EqualTo(101.0));
    }

    [Test]
    public void SetTimeMovesTheTotalWhileStopped()
    {
        _chronometer.SetTime(7.0);
        _clock.Advance(1.0);

        Assert.That(_chronometer.TotalSeconds, Is.EqualTo(7.0));
    }

    [Test]
    public void StartingAgainWhileRunningKeepsTheTime()
    {
        _chronometer.Start();
        _clock.Advance(3.0);

        _chronometer.Start();
        _clock.Advance(2.0);

        Assert.That(_chronometer.TotalSeconds, Is.EqualTo(5.0));
    }

    [Test]
    public void StopHoldsTheTotalAndStartResumesIt()
    {
        _chronometer.Start();
        _clock.Advance(10.0);
        _chronometer.Stop();
        _clock.Advance(4.0);

        Assert.That(_chronometer.IsRunning, Is.False);
        Assert.That(_chronometer.TotalSeconds, Is.EqualTo(10.0));

        _chronometer.Start();
        _clock.Advance(1.0);

        Assert.That(_chronometer.TotalSeconds, Is.EqualTo(11.0));
    }

    [Test]
    public void ResetClearsTheTotalAndKeepsRunning()
    {
        _chronometer.Start();
        _clock.Advance(5.0);

        _chronometer.Reset();
        _clock.Advance(2.0);

        Assert.That(_chronometer.TotalSeconds, Is.EqualTo(2.0));
        Assert.That(_chronometer.IsRunning, Is.True);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(double seconds) => _now += TimeSpan.FromSeconds(seconds);
    }
}
