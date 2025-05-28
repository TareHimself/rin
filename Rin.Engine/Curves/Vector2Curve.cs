using System.Numerics;

namespace Rin.Engine.Curves;

public class Vector2Curve : AdvancedCurve<Vector2>
{
    protected override Vector2 LinearInterpolateValue(in Vector2 previous, in Vector2 next, float alpha)
    {
        var dist = next - previous;
        return previous + dist * alpha;
    }
}