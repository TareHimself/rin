using Rin.Core.Shared.Time;

namespace Rin.Core.Tests.Shared.Time;

public class ChronometerTests
{
    [Test]
    public void SetTimeMovesTheTotalWhileRunning()
    {
        var chronometer = new Chronometer();
        chronometer.Start();

        chronometer.SetTime(100.0);

        Assert.That(chronometer.TotalSeconds, Is.EqualTo(100.0).Within(0.5));
        Assert.That(chronometer.IsRunning, Is.True);
    }

    [Test]
    public void StartingAgainWhileRunningKeepsTheTime()
    {
        var chronometer = new Chronometer();
        chronometer.Start();
        chronometer.SetTime(100.0);

        chronometer.Start();

        Assert.That(chronometer.TotalSeconds, Is.EqualTo(100.0).Within(0.5));
    }

    [Test]
    public void StopHoldsTheTotalAndStartResumesIt()
    {
        var chronometer = new Chronometer();
        chronometer.Start();
        chronometer.SetTime(10.0);
        chronometer.Stop();

        Assert.That(chronometer.IsRunning, Is.False);
        Assert.That(chronometer.TotalSeconds, Is.EqualTo(10.0).Within(0.5));

        chronometer.Start();

        Assert.That(chronometer.TotalSeconds, Is.EqualTo(10.0).Within(0.5));
    }
}
