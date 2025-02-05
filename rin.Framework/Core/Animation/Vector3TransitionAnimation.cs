using System.Numerics;

namespace rin.Framework.Core.Animation;

public class Vector3TransitionAnimation(Func<Vector3> from,Vector3 to,Action<Vector3> setter,double duration = 0.0,Func<double,double>? easingFunction = null) : TransitionAnimation<Vector3>(from,to,setter,duration,easingFunction)
{
    protected override Vector3 ApplyAlpha(in Vector3 diff, double alpha)
    {
        return diff * (float)alpha;
    }

    protected override Vector3 Add(in Vector3 a, in Vector3 b)
    {
        return a + b;
    }

    protected override Vector3 Subtract(in Vector3 a, in Vector3 b)
    {
        return a - b;
    }
}