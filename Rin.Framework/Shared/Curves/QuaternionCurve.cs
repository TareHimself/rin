using System.Numerics;

namespace Rin.Framework.Shared.Curves;

public class QuaternionCurve : AdvancedCurve<Quaternion>
{
    protected override Quaternion LinearInterpolateValue(in Quaternion previous, in Quaternion next, float alpha)
    {
        var dist = next - previous;
        return previous + dist * alpha;
    }
}