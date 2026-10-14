using System.Drawing;
using System.Numerics;
using JetBrains.Annotations;

namespace Rin.Core.Views;

[NoReorder]
public struct Color(float inR, float inG, float inB, float inA) : ISubtractionOperators<Color, Color, Color>,
    IMultiplyOperators<Color, float, Color>, IAdditionOperators<Color, Color, Color>
{
    public static Color Red => new(1f, 0f, 0f, 1f);
    public static Color Green => new(0f, 1f, 0f, 1f);
    public static Color Blue => new(0f, 0f, 1f, 1f);
    public static Color White => new(1f, 1f, 1f, 1f);
    public static Color Black => new(0f, 0f, 0f, 1f);
    public static Color Transparent => new(0f, 0f, 0f, 0f);

    [PublicAPI] public float R = inR;
    [PublicAPI] public float G = inG;
    [PublicAPI] public float B = inB;
    [PublicAPI] public float A = inA;

    public Color(float data) : this(data, data, data, data)
    {
    }

    public Color(float color, float alpha) : this(color, color, color, alpha)
    {
    }

    public static implicit operator Vector4(Color color)
    {
        return new Vector4(color.R, color.G, color.B, color.A);
    }

    public static implicit operator Vector3(Color color)
    {
        return new Vector3(color.R, color.G, color.B);
    }

    public static Color FromRgb(float r, float g, float b)
    {
        return new Color(r, g, b, 1f);
    }

    public static Color FromBytes(byte r, byte g, byte b, byte a = 255)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    /// <summary>
    ///     Hue wraps, so 0 and 1 are both red and the whole range stays on the wheel.
    /// </summary>
    public static Color FromHsv(float hue, float saturation, float value, float alpha = 1f)
    {
        var sector = (hue - MathF.Floor(hue)) * 6f;
        var chroma = value * saturation;
        var secondary = chroma * (1f - MathF.Abs(sector % 2f - 1f));
        var (r, g, b) = (int)sector switch
        {
            0 => (chroma, secondary, 0f),
            1 => (secondary, chroma, 0f),
            2 => (0f, chroma, secondary),
            3 => (0f, secondary, chroma),
            4 => (secondary, 0f, chroma),
            _ => (chroma, 0f, secondary)
        };
        var lift = value - chroma;
        return new Color(r + lift, g + lift, b + lift, alpha);
    }

    public static Color FromHtml(string hexCode)
    {
        var c = ColorTranslator.FromHtml(hexCode);
        return FromBytes(c.R, c.G, c.B, c.A);
    }

    public static Color operator -(Color left, Color right)
    {
        return new Color(left.R - right.R, left.G - right.G, left.B - right.B, left.A - right.A);
    }

    public static Color operator *(Color left, float right)
    {
        return new Color(left.R * right, left.G * right, left.B * right, left.A * right);
    }

    public static Color operator +(Color left, Color right)
    {
        return new Color(left.R + right.R, left.G + right.G, left.B + right.B, left.A + right.A);
    }
}