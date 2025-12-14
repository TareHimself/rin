using System.Numerics;

namespace Rin.Framework.Animation;

public class Vector4TransitionAnimation(
    Func<Vector4> from,
    Vector4 to,
    Action<Vector4> setter,
    float duration = 0f,
    Func<float, float>? easingFunction = null)
    : TransitionAnimation<Vector4>(from, to, setter, duration, easingFunction)
{
    protected override Vector4 ApplyAlpha(in Vector4 diff, float alpha)
    {
        return diff * alpha;
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