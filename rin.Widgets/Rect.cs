using rin.Core;
using rin.Core.Math;

namespace rin.Widgets;

public class Rect : ICloneable<Rect>
{
    public Vector2<float> Offset;
    public Vector2<float> Size;

    public Rect()
    {
        Offset = new Vector2<float>(0, 0);
        Size = new Vector2<float>();
    }

    public Rect(Vector2<float> inOffset, Vector2<float> inSize)
    {
        Offset = inOffset;
        Size = inSize;
    }

    public Rect Clone()
    {
        return new Rect(Offset.Clone(), Size.Clone());
    }

    public static implicit operator Vector4<float>(Rect rect)
    {
        return new Vector4<float>(rect.Offset.X, rect.Offset.Y, rect.Size.X,
            rect.Size.X);
    }

    public bool IntersectsWith(Rect rect)
    {
        var a1 = Offset;
        var a2 = a1 + Size;
        var b1 = rect.Offset;
        var b2 = b1 + rect.Size;

        if (a1.X <= b1.X)
        {
            if (a1.Y <= b1.Y)
                return b1.X <= a2.X && b1.Y <= a2.Y; // A top left B bottom right
            return b1.X <= a2.X && a1.Y <= b2.Y; // A Bottom left B Top right
        }

        if (a1.Y <= b1.Y)
            return a1.X <= b2.X && b1.Y <= a2.Y; // A top right B bottom left
        return a1.X <= b2.X && a1.Y <= b2.Y; // A bottom right B top left
    }

    /// <summary>
    ///     Clamps this rect to the specified area
    /// </summary>
    /// <param name="area"></param>
    /// <returns></returns>
    public Rect Clamp(Rect area)
    {
        if (!IntersectsWith(area))
        {
            Offset = area.Offset.Clone();
            Size = new Vector2<float>();
            return this;
        }

        var a1 = Offset;
        var a2 = a1 + Size;
        var b1 = area.Offset;
        var b2 = b1 + area.Size;

        Offset = new Vector2<float>(System.Math.Max(a1.X, b1.X), System.Math.Max(a1.Y, b1.Y));

        var p2 = new Vector2<float>(System.Math.Min(a2.X, b2.X), System.Math.Min(a2.Y, b2.Y));

        Size = p2 - Offset;

        return this;
    }

    public static implicit operator Pair<Vector2<float>, Vector2<float>>(Rect rect)
    {
        return new Pair<Vector2<float>, Vector2<float>>(rect.Offset, rect.Offset + rect.Size);
    }
}