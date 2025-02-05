using System.Numerics;

namespace rin.Framework.Core.Animation;

public class Vector4TransitionAnimation(Func<Vector4> from,Vector4 to,Action<Vector4> setter,double duration = 0.0,Func<double,double>? easingFunction = null) : TransitionAnimation<Vector4>(from,to,setter,duration,easingFunction)
{
    protected override Vector4 ApplyAlpha(in Vector4 diff, double alpha)
    {
        return diff * (float)alpha;
    }

    protected override Vector4 Add(in Vector4 a, in Vector4 b)
    {
        return a + b;
    }

    protected override Vector4 Subtract(in Vector4 a, in Vector4 b)
    {
        return a - b;
    }
}