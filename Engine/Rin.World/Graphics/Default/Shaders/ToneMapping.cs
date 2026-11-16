using System.Numerics;
using Rin.Shade;
using static Rin.Shade.Shader;

namespace Rin.World.Graphics.Default.Shaders;

/// <summary>
///     The curves behind <see cref="ToneMapCurve" />. Each takes linear scene colour and returns linear display colour
///     in 0 to 1.
/// </summary>
[ShadeExport]
public static class ToneMapping
{
    public static Vector3 Apply(Vector3 color, ToneMapCurve curve)
    {
        return curve switch
        {
            ToneMapCurve.Neutral => Neutral(color),
            ToneMapCurve.Aces => Aces(color),
            _ => AgX(color)
        };
    }

    /// <summary>
    ///     AgX as in Blender, Filament and three.js: mix the channels a little, take a log, apply an S curve, then undo
    ///     the mix. Working in the wide Rec. 2020 colour space is what lets bright colours fade to white.
    /// </summary>
    public static Vector3 AgX(Vector3 color)
    {
        const float minExposure = -12.47393f;
        const float maxExposure = 4.026069f;

        var wide = Mix(color,
            new Vector3(0.6274f, 0.0691f, 0.0164f),
            new Vector3(0.3293f, 0.9195f, 0.0880f),
            new Vector3(0.0433f, 0.0113f, 0.8956f));
        var inset = Mix(wide,
            new Vector3(0.856627153315983f, 0.137318972929847f, 0.11189821299995f),
            new Vector3(0.0951212405381588f, 0.761241990602591f, 0.0767994186031903f),
            new Vector3(0.0482516061458583f, 0.101439036467562f, 0.811302368396859f));

        var exposure = Log2(Max(inset, new Vector3(1e-10f)));
        var encoded = Saturate((exposure - new Vector3(minExposure)) / (maxExposure - minExposure));
        var curved = AgXContrast(encoded);

        var outset = Mix(curved,
            new Vector3(1.1271005818144368f, -0.1413297634984383f, -0.14132976349843826f),
            new Vector3(-0.11060664309660323f, 1.157823702216272f, -0.11060664309660294f),
            new Vector3(-0.016493938717834573f, -0.016493938717834257f, 1.2519364065950405f));
        var linear = Pow(Max(outset, new Vector3(0f)), new Vector3(2.2f));

        return Saturate(Mix(linear,
            new Vector3(1.6605f, -0.1246f, -0.0182f),
            new Vector3(-0.5876f, 1.1329f, -0.1006f),
            new Vector3(-0.0728f, -0.0083f, 1.1187f)));
    }

    // Krzysztof Narkowicz's fit of the ACES reference curve.
    public static Vector3 Aces(Vector3 color)
    {
        var numerator = color * (color * 2.51f + new Vector3(0.03f));
        var denominator = color * (color * 2.43f + new Vector3(0.59f)) + new Vector3(0.14f);
        return Saturate(numerator / denominator);
    }

    // The Khronos PBR Neutral tone mapper.
    public static Vector3 Neutral(Vector3 color)
    {
        const float compressionStart = 0.76f;
        const float compressionRange = 1f - compressionStart;
        const float desaturation = 0.15f;

        var darkest = Min(color.X, Min(color.Y, color.Z));
        var offset = darkest < 0.08f ? darkest - 6.25f * darkest * darkest : 0.04f;
        var shifted = color - new Vector3(offset);

        var peak = Max(shifted.X, Max(shifted.Y, shifted.Z));
        if (peak < compressionStart) return shifted;

        var compressedPeak = 1f - compressionRange * compressionRange / (peak + compressionRange - compressionStart);
        var compressed = shifted * (compressedPeak / peak);
        var whiteness = 1f - 1f / (desaturation * (peak - compressedPeak) + 1f);
        return Lerp(compressed, new Vector3(compressedPeak), whiteness);
    }

    /// <summary>
    ///     A colour matrix written as what each input channel contributes to the output.
    /// </summary>
    private static Vector3 Mix(Vector3 color, Vector3 fromRed, Vector3 fromGreen, Vector3 fromBlue)
    {
        return fromRed * color.X + fromGreen * color.Y + fromBlue * color.Z;
    }

    // A polynomial fit of the AgX S curve by Benjamin Wrensch, for inputs in 0 to 1.
    private static Vector3 AgXContrast(Vector3 x)
    {
        var x2 = x * x;
        var x4 = x2 * x2;
        return x4 * x2 * 15.5f
               - x4 * x * 40.14f
               + x4 * 31.96f
               - x2 * x * 6.868f
               + x2 * 0.4298f
               + x * 0.1191f
               - new Vector3(0.00232f);
    }
}
