using System.Drawing;
using System.Numerics;

namespace Rin.Engine.Views;

public struct Color(float inR, float inG, float inB, float inA) : ISubtractionOperators<Color, Color, Color>,
    IMultiplyOperators<Color, float, Color>, IAdditionOperators<Color, Color, Color>
{
    public static Color Red = new(1f, 0f, 0f, 1f);
    public static Color Green = new(0f, 1f, 0f, 1f);
    public static Color Blue = new(0f, 0f, 1f, 1f);
    public static Color White = new(1f, 1f, 1f, 1f);
    public static Color Black = new(0f, 0f, 0f, 1f);
    public static Color Transparent = new(0f, 0f, 0f, 0f);

    public float A = inA;
    public float B = inB;
    public float G = inG;
    public float R = inR;

    public Color(float data) : this(data, data, data, data)
    {
    }

    public Color Clone(float? r = null, float? g = null, float? b = null, float? a = null)
    {
        return new Color(r ?? R, g ?? G, b ?? B, a ?? A);
    }

    public static implicit operator Vector4(Color color)
    {
        return new Vector4(color.R, color.G, color.B, color.A);
    }

    public static implicit operator Vector3(Color color)
    {
        return new Vector3(color.R, color.G, color.B);
    }

    public static Color FromHtml(string hexCode)
    {
        var c = ColorTranslator.FromHtml(hexCode);
        return new Color(Convert.ToInt16(c.R), Convert.ToInt16(c.G), Convert.ToInt16(c.B), Convert.ToInt16(c.A));
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