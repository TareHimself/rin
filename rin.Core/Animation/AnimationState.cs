namespace rin.Core.Animation;

public class AnimationState
{
    public required double StartTime;
    public required IAnimation Animation;
    public double Duration => Animation.Duration;
    public bool Active = false;

    public bool Update(double elapsed)
    {
        var animElapsed = elapsed - StartTime;
        if (!Active)
        {
            Active = true;
            Animation.Start(animElapsed);
        }
        Animation.Update(System.Math.Min(animElapsed, Duration));
        return animElapsed >= Duration;
    }

    // public bool Update(double start, double current)
    // {
    //     var actualStart = start + _elapsedTimeFromPrevious;
    //     
    //     if (_current == null)
    //     {
    //         if (!_active)
    //         {
    //             _current = InitialAnimation;
    //             _current.Start(actualStart,current);
    //             return false;
    //             _active = true;
    //         }
    //         else
    //         {
    //             return true;   
    //         }
    //     }
    //
    //     
    //
    //     if (!_current.Update(actualStart, current)) return false;
    //     
    //     if (_current.Next is {} asNext)
    //     {
    //         _elapsedTimeFromPrevious += actualStart;
    //         actualStart = start + _elapsedTimeFromPrevious;
    //         _current = asNext;
    //         _current.Start(actualStart,current);
    //         return false;
    //     }
    //
    //     _current = null;
    //     return true;
    //
    // }
}