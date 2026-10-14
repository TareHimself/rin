using System.Numerics;
using Rin.Shade;
using static Rin.Shade.Shader;

namespace Rin.Core.Views.Graphics.Shaders;

// https://iquilezles.org/articles/distfunctions2d/
public static class Sd
{
    public static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        var pa = p - a;
        var ba = b - a;
        var h = Clamp(Dot(pa, ba) / Dot(ba, ba), 0f, 1f);
        return Length(pa - ba * h);
    }

    public static float Circle(Vector2 location, Vector2 center, float radius)
    {
        return Length(location - center) - radius;
    }

    public static float Bezier(Vector2 pos, Vector2 begin, Vector2 end, Vector2 controlPoint)
    {
        var a = controlPoint - begin;
        var control = begin - 2f * controlPoint + end;
        var b = a * 2f;
        var d = begin - pos;
        var kk = 1f / Dot(control, control);
        var kx = kk * Dot(a, control);
        var ky = kk * (2f * Dot(a, a) + Dot(d, control)) / 3f;
        var kz = kk * Dot(d, a);
        var res = 0f;
        var p = ky - kx * kx;
        var p3 = p * p * p;
        var q = kx * (2f * kx * kx - 3f * ky) + kz;
        var h = q * q + 4f * p3;
        if (h >= 0f)
        {
            h = Sqrt(h);
            var x = (new Vector2(h, -h) - new Vector2(q)) / 2f;
            var uv = Sign(x) * Pow(Abs(x), new Vector2(1f / 3f));
            var t = Clamp(uv.X + uv.Y - kx, 0f, 1f);
            var offset = d + (b + control * t) * t;
            res = Dot(offset, offset);
        }
        else
        {
            var z = Sqrt(-p);
            var v = Acos(q / (p * z * 2f)) / 3f;
            var m = Cos(v);
            var n = Sin(v) * 1.732050808f;
            var t = Clamp(new Vector3(m + m, -n - m, n - m) * z - new Vector3(kx), new Vector3(0f),
                new Vector3(1f));
            var first = d + (b + control * t.X) * t.X;
            var second = d + (b + control * t.Y) * t.Y;
            res = Min(Dot(first, first), Dot(second, second));
        }

        return Sqrt(res);
    }

    public static float Cubic(Vector2 position, Vector2 begin, Vector2 end, Vector2 controlA, Vector2 controlB)
    {
        return Sqrt(CubicDistanceSquared(position, begin, controlA, controlB, end));
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

            t = Clamp(t, 0f, 1f);
            var toPoint = ((a3 * t + a2) * t + a1) * t + a0;
            best = Min(best, Dot(toPoint, toPoint));
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
        return t - Dot(tangent, toPoint) / Dot(tangent, tangent);
    }
}
