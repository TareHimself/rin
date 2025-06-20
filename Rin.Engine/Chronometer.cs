namespace Rin.Engine;


public class Chronometer : IChronometer
{
    
    private double _accumulatedTime;
    private DateTime _startedAt = DateTime.UtcNow;
    
    public double ElapsedTime => (DateTime.UtcNow - _startedAt).TotalSeconds;
    public bool IsRunning { get; private set; }
    
    public double TotalSeconds => IsRunning ? _accumulatedTime + ElapsedTime : _accumulatedTime;

    public void Start()
    {
        _startedAt = DateTime.UtcNow;
        IsRunning = true;
    }

    public void Stop()
    {
        if(!IsRunning) return;
        IsRunning = false;
        _accumulatedTime += ElapsedTime;
    }

    public void Reset()
    {
        if (IsRunning)
        {
            _startedAt = DateTime.UtcNow;
        }
        _accumulatedTime = 0;
    }

    public void SetTime(double seconds)
    {
        var now = DateTime.UtcNow;
        var elapsed = IsRunning ? (now - _startedAt).TotalSeconds : 0.0;
        var total = _accumulatedTime + elapsed;
        var delta = seconds - total;
        _accumulatedTime += delta;
    }
}