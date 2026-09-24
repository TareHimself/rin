using System.Numerics;

namespace Rin.Core.Shared.Curves;

public class QuaternionCurve : AdvancedCurve<Quaternion>
{
    protected override Quaternion LinearInterpolateValue(in Quaternion previous, in Quaternion next, float alpha)
    {
        return Quaternion.Slerp(previous, next, alpha);
    }
}