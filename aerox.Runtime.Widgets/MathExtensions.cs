using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets;

public static class MathExtensions
{
    public static Vector2<float> ApplyTransformation(this Vector2<float> src,Matrix3 matrix)
    {
        var vector3 = new Vector3<float>(src.X, src.Y, 1.0f);
        var res = matrix * vector3;
        return new Vector2<float>(res.X, res.Y);
    }
}