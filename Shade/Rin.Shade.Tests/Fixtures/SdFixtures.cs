using System.Numerics;
using Rin.Shade;

namespace Rin.Shade.Tests.Fixtures;

public static class Intrinsics
{
    [SlangExpression("dot(@0, @1)")] public static extern float Dot(Vector2 a, Vector2 b);
    [SlangExpression("length(@0)")] public static extern float Length(Vector2 v);
    [SlangExpression("clamp(@0, @1, @2)")] public static extern float Clamp(float x, float min, float max);
}

public static class Sd
{
    public static float Dot2(Vector2 v) => Intrinsics.Dot(v, v);

    public static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var pa = p - a;
        var ba = b - a;
        var h = Intrinsics.Clamp(Intrinsics.Dot(pa, ba) / Dot2(ba), 0f, 1f);
        return Intrinsics.Length(pa - ba * h);
    }

    // Deliberately unused by SdfFixtureShader - proves dead-code elimination: this must NOT appear
    // in emitted output.
    public static float SdCircle(Vector2 location, Vector2 center, float r) =>
        Intrinsics.Length(location - center) - r;
}
