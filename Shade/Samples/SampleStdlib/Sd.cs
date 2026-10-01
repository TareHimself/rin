using System.Numerics;
using Rin.Shade;

namespace SampleStdlib;

public static class Intrinsics
{
    [SlangExpression("dot(@0, @1)")] public static extern float Dot(Vector2 a, Vector2 b);
    [SlangExpression("length(@0)")] public static extern float Length(Vector2 v);
}

[ShadeExport]
public static class Sd
{
    public static float Dot2(Vector2 v) => Intrinsics.Dot(v, v);

    public static float SdCircle(Vector2 location, Vector2 center, float r) =>
        Intrinsics.Length(location - center) - r;

    public static float SdCircleSquared(Vector2 location, Vector2 center, float r) =>
        MathHelpers.Square(SdCircle(location, center, r));
}
