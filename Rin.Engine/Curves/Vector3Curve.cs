using System.Numerics;

namespace Rin.Engine.Curves;

public class Vector3Curve : AdvancedCurve<Vector3>
{
    protected override Vector3 LinearInterpolateValue(in Vector3 previous, in Vector3 next, float alpha)
    {
        var dist = next - previous;
        return previous + dist * alpha;
    }
}