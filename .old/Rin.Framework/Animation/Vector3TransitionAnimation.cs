using System.Numerics;

namespace Rin.Framework.Animation;

public class Vector3TransitionAnimation(
    Func<Vector3> from,
    Vector3 to,
    Action<Vector3> setter,
    float duration = 0f,
    Func<float, float>? easingFunction = null)
    : TransitionAnimation<Vector3>(from, to, setter, duration, easingFunction)
{
    protected override Vector3 ApplyAlpha(in Vector3 diff, float alpha)
    {
        return diff * alpha;
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