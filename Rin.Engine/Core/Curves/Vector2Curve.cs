using System.Numerics;

namespace Rin.Engine.Core.Curves;

public class Vector2Curve : Curve<Vector2>
{
    protected override Vector2 Interpolate(float alpha, Vector2 previous, Vector2 next)
    {
        var dist = next - previous;
        return previous + dist * alpha;
    }
}