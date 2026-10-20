using System.Diagnostics;

namespace Rin.Core.Graphics;

/// <summary>
///     Paces a loop to at most <see cref="MaxFramerate" /> iterations per second. <see cref="Wait" /> sleeps for
///     most of the time left in the current interval and spins for the last millisecond, so the cap holds without
///     drifting on the coarse timer that <see cref="Thread.Sleep(int)" /> uses.
/// </summary>
public sealed class FrameLimiter
{
    private const long SpinWindowMilliseconds = 1;

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private long _nextFrameTicks;

    /// <summary>
    ///     Frames per second, or 0 for no cap.
    /// </summary>
    public int MaxFramerate { get; set; }

    public void Wait()
    {
        var max = MaxFramerate;
        if (max <= 0) return;

        var interval = Stopwatch.Frequency / max;
        var now = _clock.ElapsedTicks;
        if (now < _nextFrameTicks)
        {
            SleepUntil(_nextFrameTicks);
            now = _clock.ElapsedTicks;
        }

        // The next frame is one interval after whichever came later, the schedule or this frame, so a late frame
        // does not carry its debt into a burst of fast frames.
        _nextFrameTicks = long.Max(_nextFrameTicks, now) + interval;
    }

    private void SleepUntil(long targetTicks)
    {
        var spinTicks = Stopwatch.Frequency * SpinWindowMilliseconds / 1000;
        while (true)
        {
            var remaining = targetTicks - _clock.ElapsedTicks;
            if (remaining <= 0) return;

            if (remaining > spinTicks)
                Thread.Sleep((int)((remaining - spinTicks) * 1000 / Stopwatch.Frequency));
            else
                Thread.Yield();
        }
    }
}
