using System.Numerics;

namespace Rin.Engine.Animation;

public class Vector2TransitionAnimation(
    Func<Vector2> from,
    Vector2 to,
    Action<Vector2> setter,
    float duration = 0f,
    Func<float, float>? easingFunction = null)
    : TransitionAnimation<Vector2>(from, to, setter, duration, easingFunction)
{
    protected override Vector2 ApplyAlpha(in Vector2 diff, float alpha)
    {
        return diff * alpha;
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