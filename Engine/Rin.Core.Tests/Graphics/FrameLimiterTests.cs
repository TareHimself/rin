using System.Diagnostics;
using Rin.Core.Graphics;

namespace Rin.Core.Tests.Graphics;

public class FrameLimiterTests
{
    [Test]
    public void NoCapReturnsAtOnce()
    {
        var limiter = new FrameLimiter();
        var clock = Stopwatch.StartNew();

        for (var i = 0; i < 100; i++) limiter.Wait();

        Assert.That(clock.ElapsedMilliseconds, Is.LessThan(50));
    }

    [Test]
    public void CapHoldsTheFramesToTheInterval()
    {
        var limiter = new FrameLimiter { MaxFramerate = 100 };
        limiter.Wait();
        var clock = Stopwatch.StartNew();

        for (var i = 0; i < 10; i++) limiter.Wait();

        Assert.That(clock.ElapsedMilliseconds, Is.InRange(90, 400));
    }

    [Test]
    public void ALateFrameDoesNotCauseABurst()
    {
        var limiter = new FrameLimiter { MaxFramerate = 100 };
        limiter.Wait();
        Thread.Sleep(60);
        limiter.Wait();
        var clock = Stopwatch.StartNew();

        limiter.Wait();

        Assert.That(clock.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void RemovingTheCapStopsTheWaiting()
    {
        var limiter = new FrameLimiter { MaxFramerate = 10 };
        limiter.Wait();
        limiter.MaxFramerate = 0;
        var clock = Stopwatch.StartNew();

        limiter.Wait();

        Assert.That(clock.ElapsedMilliseconds, Is.LessThan(50));
    }
}
