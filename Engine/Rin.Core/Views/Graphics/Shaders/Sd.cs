using System.Numerics;
using Rin.Shade;
using ShadeMath = Rin.Shade.Shader.Math;

namespace Rin.Core.Views.Graphics.Shaders;

// https://iquilezles.org/articles/distfunctions2d/
public static class Sd
{
    public static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        var pa = p - a;
        var ba = b - a;
        var h = ShadeMath.Clamp(ShadeMath.Dot(pa, ba) / ShadeMath.Dot(ba, ba), 0f, 1f);
        return ShadeMath.Length(pa - ba * h);
    }

    public static float Circle(Vector2 location, Vector2 center, float radius)
    {
        return ShadeMath.Length(location - center) - radius;
    }

    public static float Bezier(Vector2 pos, Vector2 begin, Vector2 end, Vector2 controlPoint)
    {
        var a = controlPoint - begin;
        var control = begin - 2f * controlPoint + end;
        var b = a * 2f;
        var d = begin - pos;
        var kk = 1f / ShadeMath.Dot(control, control);
        var kx = kk * ShadeMath.Dot(a, control);
        var ky = kk * (2f * ShadeMath.Dot(a, a) + ShadeMath.Dot(d, control)) / 3f;
        var kz = kk * ShadeMath.Dot(d, a);
        var res = 0f;
        var p = ky - kx * kx;
        var p3 = p * p * p;
        var q = kx * (2f * kx * kx - 3f * ky) + kz;
        var h = q * q + 4f * p3;
        if (h >= 0f)
        {
            h = ShadeMath.Sqrt(h);
            var x = (new Vector2(h, -h) - new Vector2(q)) / 2f;
            var uv = ShadeMath.Sign(x) * ShadeMath.Pow(ShadeMath.Abs(x), new Vector2(1f / 3f));
            var t = ShadeMath.Clamp(uv.X + uv.Y - kx, 0f, 1f);
            var offset = d + (b + control * t) * t;
            res = ShadeMath.Dot(offset, offset);
        }
        else
        {
            var z = ShadeMath.Sqrt(-p);
            var v = ShadeMath.Acos(q / (p * z * 2f)) / 3f;
            var m = ShadeMath.Cos(v);
            var n = ShadeMath.Sin(v) * 1.732050808f;
            var t = ShadeMath.Clamp(new Vector3(m + m, -n - m, n - m) * z - new Vector3(kx), new Vector3(0f),
                new Vector3(1f));
            var first = d + (b + control * t.X) * t.X;
            var second = d + (b + control * t.Y) * t.Y;
            res = ShadeMath.Min(ShadeMath.Dot(first, first), ShadeMath.Dot(second, second));
        }

        return ShadeMath.Sqrt(res);
    }

    public static float Cubic(Vector2 position, Vector2 begin, Vector2 end, Vector2 controlA, Vector2 controlB)
    {
        return ShadeMath.Sqrt(CubicDistanceSquared(position, begin, controlA, controlB, end));
    }

    private static float CubicDistanceSquared(Vector2 uv, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var a3 = -p0 + 3f * p1 - 3f * p2 + p3;
        var a2 = 3f * p0 - 6f * p1 + 3f * p2;
        var a1 = -3f * p0 + 3f * p1;
        var a0 = p0 - uv;

        var best = 1e38f;
        var start = 0f;
        for (var i = 0; i < 3; i++)
        {
            var t = start;
            for (var j = 0; j < 3; j++) t = CubicNormalIteration(t, a0, a1, a2, a3);

            t = ShadeMath.Clamp(t, 0f, 1f);
            var toPoint = ((a3 * t + a2) * t + a1) * t + a0;
            best = ShadeMath.Min(best, ShadeMath.Dot(toPoint, toPoint));
            start += 0.5f;
        }

        return best;
    }

    private static float CubicNormalIteration(float t, Vector2 a0, Vector2 a1, Vector2 a2, Vector2 a3)
    {
        var a2t = a2 + t * a3;
        var a1t = a1 + t * a2t;
        var b2 = a2t + t * a3;

        var toPoint = a0 + t * a1t;
        var tangent = a1t + t * b2;
        return t - ShadeMath.Dot(tangent, toPoint) / ShadeMath.Dot(tangent, tangent);
    }
}
