using System.Drawing;
using rin.Core.Math;

namespace rin.Widgets;

public class Color(float inR, float inG, float inB, float inA)
{
    public static Color Red = new(1f, 0f, 0f, 1f);
    public static Color Green = new(0f, 1f, 0f, 1f);
    public static Color Blue = new(0f, 0f, 1f, 1f);
    public static Color White = new(1f, 1f, 1f, 1f);
    public static Color Black = new(0f, 0f, 0f, 1f);

    public float A = inA;
    public float B = inB;
    public float G = inG;
    public float R = inR;

    public Color(float data) : this(data, data, data, data)
    {
    }

    public Color Clone(float? r = null,float? g = null,float? b = null,float? a = null)
    {
        return new Color(r ?? R,g ?? G,b ?? B,a ?? A);
    }


    public static implicit operator Vector4<float>(Color color)
    {
        return new Vector4<float>(color.R, color.G, color.B, color.A);
    }

    public static Color FromHex(string hexCode)
    {
        var c = ColorTranslator.FromHtml(hexCode);
        return new Color(Convert.ToInt16(c.R), Convert.ToInt16(c.G), Convert.ToInt16(c.B), Convert.ToInt16(c.A));
    }
}