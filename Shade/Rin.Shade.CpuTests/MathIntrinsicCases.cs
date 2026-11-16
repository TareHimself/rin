using System.Numerics;
using static Rin.Shade.Shader;

namespace Rin.Shade.CpuTests;

/// <summary>
///     One source for both sides of <see cref="MathIntrinsicTests" />: compiled into this assembly, and handed to the
///     transpiler as text.
/// </summary>
public static class MathIntrinsicCases
{
    public const int Count = 44;

    /// <summary>
    ///     One math call per <paramref name="id" />, for <paramref name="x" /> and <paramref name="y" /> between 0 and 1.
    /// </summary>
    public static float Evaluate(int id, float x, float y)
    {
        var signed = (x - 0.5f) * 7f;
        var v = new Vector3(x, y, x * y + 0.3f);
        var w = new Vector3(y, 0.4f, x);

        return id switch
        {
            0 => Abs(signed),
            1 => Sign(signed),
            2 => Floor(signed),
            3 => Ceil(signed),
            4 => Round(signed),
            5 => Frac(signed),
            6 => Sqrt(x),
            7 => Rsqrt(x),
            8 => Saturate(signed),
            9 => Exp(signed),
            10 => Exp2(signed),
            11 => Log(x),
            12 => Log2(x),
            13 => Sin(signed),
            14 => Cos(signed),
            15 => Tan(x),
            16 => Asin(x),
            17 => Acos(x),
            18 => Atan(signed),
            19 => Radians(signed),
            20 => Degrees(signed),
            21 => Min(x, y),
            22 => Max(x, y),
            23 => Pow(x, y),
            24 => Step(x, y),
            25 => Atan2(signed, y),
            26 => Clamp(signed, x, y + 1f),
            27 => SmoothStep(0.2f, 0.8f, x),
            28 => Lerp(x, y, 0.3f),
            29 => Normalize(v).X,
            30 => Normalize(v).Z,
            31 => Length(v),
            32 => Dot(v, w),
            33 => Distance(v, w),
            34 => Reflect(v, Normalize(w)).Y,
            35 => Refract(Normalize(v), Normalize(w), 0.7f).X,
            36 => Cross(v, w).Z,
            37 => Abs(new Vector3(signed, -x, y)).Y,
            38 => Lerp(v, w, 0.25f).Z,
            39 => Clamp(v, new Vector3(0.2f), new Vector3(0.6f)).X,
            40 => Max(v, w).Y,
            41 => Saturate(new Vector2(signed, x)).X,
            42 => SmoothStep(new Vector3(0.2f), new Vector3(0.8f), v).Z,
            _ => Step(v, w).X
        };
    }
}
