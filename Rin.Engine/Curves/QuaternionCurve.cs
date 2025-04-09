using System.Numerics;

namespace Rin.Engine.Curves;

public class QuaternionCurve : Curve<Quaternion,Quaternion>
{
    
    protected override Quaternion Interpolate(in Quaternion previous, in Quaternion next, float alpha)
    {
        var dist = next - previous;
        return previous + dist * alpha;
    }

    protected override Quaternion ToInterpolatedValue(in Quaternion value)
    {
        return value;
    }
}