namespace rin.Core.Animation;

public class ActiveAnimation(IAnimation initialAnimation)
{
    public readonly IAnimation InitialAnimation = initialAnimation;
    public double TotalDuration => InitialAnimation.TotalDuration;
    private double _elapsedTimeFromPrevious = 0.0;
    private IAnimation? _current = initialAnimation;

    public bool Update(double start, double current)
    {
        if (_current == null)
        {
            return true;
        }

        var actualStart = start + _elapsedTimeFromPrevious;

        if (!_current.Update(actualStart, current)) return false;
        
        if (_current.Next is {} asNext)
        {
            _elapsedTimeFromPrevious += _current.Duration;
            actualStart = start + _elapsedTimeFromPrevious;
            _current = asNext;
            _current.Update(actualStart, current);
            return false;
        }

        _current = null;
        return true;

    }
}