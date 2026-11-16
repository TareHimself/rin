namespace Rin.Core.Shared.Time;

public class Chronometer(TimeProvider? timeProvider = null) : IChronometer
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private double _accumulatedTime;
    private DateTimeOffset _startedAt = (timeProvider ?? TimeProvider.System).GetUtcNow();

    public double ElapsedTime => (_timeProvider.GetUtcNow() - _startedAt).TotalSeconds;
    public bool IsRunning { get; private set; }

    public double TotalSeconds => IsRunning ? _accumulatedTime + ElapsedTime : _accumulatedTime;

    public void Start()
    {
        if (IsRunning) return;
        _startedAt = _timeProvider.GetUtcNow();
        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        _accumulatedTime += ElapsedTime;
    }

    public void Reset()
    {
        if (IsRunning) _startedAt = _timeProvider.GetUtcNow();
        _accumulatedTime = 0;
    }

    public void SetTime(double seconds)
    {
        _accumulatedTime += seconds - TotalSeconds;
    }
}