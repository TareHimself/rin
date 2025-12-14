namespace Rin.Framework.Animation;

public class AnimationState
{
    public bool Active;
    public required IAnimation Animation;
    public required float StartTime;
    public float Duration => Animation.Duration;

    public bool Update(float elapsed)
    {
        var animElapsed = elapsed - StartTime;
        if (!Active)
        {
            Active = true;
            Animation.Start(animElapsed);
        }

        Animation.Update(float.Min(animElapsed, Duration));
        return animElapsed >= Duration;
    }

    // public bool Update(float start, float current)
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