using System.Numerics;

namespace Rin.Engine.Curves;

public class Vector3Curve : Curve<Vector3, Vector3>
{
    protected override Vector3 Interpolate(in Vector3 previous, in Vector3 next, float alpha)
    {
        var dist = next - previous;
        return previous + dist * alpha;
    }

    protected override Vector3 ToInterpolatedValue(in Vector3 value)
    {
        return value;
    }
}