using rin.Framework.Core.Math;

namespace rin.Framework.Views;

public static class MathExtensions
{
    public static Vec2<float> ApplyTransformation(this Vec2<float> src, Mat3 matrix)
    {
        var vector3 = new Vec3<float>(src.X, src.Y, 1.0f);
        var res = matrix * vector3;
        return new Vec2<float>(res.X, res.Y);
    }
    
}