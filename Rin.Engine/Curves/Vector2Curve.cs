using System.Numerics;

namespace Rin.Engine.Curves;

public class Vector2Curve : Curve<Vector2, Vector2>
{
    protected override Vector2 Interpolate(in Vector2 previous, in Vector2 next, float alpha)
    {
        var dist = next - previous;
        return previous + dist * alpha;
    }

    protected override Vector2 ToInterpolatedValue(in Vector2 value)
    {
        return value;
    }
}