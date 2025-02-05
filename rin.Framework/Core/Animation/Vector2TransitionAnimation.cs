using System.Numerics;

namespace rin.Framework.Core.Animation;

public class Vector2TransitionAnimation(Func<Vector2> from,Vector2 to,Action<Vector2> setter,double duration = 0.0,Func<double,double>? easingFunction = null) : TransitionAnimation<Vector2>(from,to,setter,duration,easingFunction)
{
    protected override Vector2 ApplyAlpha(in Vector2 diff, double alpha)
    {
        return diff * (float)alpha;
    }

    protected override Vector2 Add(in Vector2 a, in Vector2 b)
    {
        return a + b;
    }

    protected override Vector2 Subtract(in Vector2 a, in Vector2 b)
    {
        return a - b;
    }
}