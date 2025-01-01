using rin.Framework.Core;
using rin.Framework.Core.Math;

namespace rin.Framework.Views;

public class Rect : ICloneable<Rect>
{
    public Vec2<float> Offset;
    public Vec2<float> Size;

    public Rect()
    {
        Offset = new Vec2<float>(0, 0);
        Size = new Vec2<float>();
    }

    public Rect(Vec2<float> inOffset, Vec2<float> inSize)
    {
        Offset = inOffset;
        Size = inSize;
    }

    public Rect Clone()
    {
        return new Rect(Offset.Clone(), Size.Clone());
    }

    public static implicit operator Vec4<float>(Rect rect)
    {
        return new Vec4<float>(rect.Offset.X, rect.Offset.Y, rect.Size.X,
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
            Size = new Vec2<float>();
            return this;
        }

        var a1 = Offset;
        var a2 = a1 + Size;
        var b1 = area.Offset;
        var b2 = b1 + area.Size;

        Offset = new Vec2<float>(System.Math.Max(a1.X, b1.X), System.Math.Max(a1.Y, b1.Y));

        var p2 = new Vec2<float>(System.Math.Min(a2.X, b2.X), System.Math.Min(a2.Y, b2.Y));

        Size = p2 - Offset;

        return this;
    }

    public static implicit operator Pair<Vec2<float>, Vec2<float>>(Rect rect)
    {
        return new Pair<Vec2<float>, Vec2<float>>(rect.Offset, rect.Offset + rect.Size);
    }
    
    // ReSharper disable once InconsistentNaming
    public static Rect MakeAABB(Vec2<float> size,Mat3 transform,Vec2<float>? offset)
    {
        var tl = offset.GetValueOrDefault(new Vec2<float>(0.0f));
        var br = tl + size;
        var tr = new Vec2<float>(br.X, tl.Y);
        var bl = new Vec2<float>(tl.X, br.Y);

        tl = tl.ApplyTransformation(transform);
        br = br.ApplyTransformation(transform);
        tr = tr.ApplyTransformation(transform);
        bl = bl.ApplyTransformation(transform);

        var p1AABB = new Vec2<float>(
            System.Math.Min(
                System.Math.Min(tl.X, tr.X),
                System.Math.Min(bl.X, br.X)
            ),
            System.Math.Min(
                System.Math.Min(tl.Y, tr.Y),
                System.Math.Min(bl.Y, br.Y)
            )
        );
        var p2AABB = new Vec2<float>(
            System.Math.Max(
                System.Math.Max(tl.X, tr.X),
                System.Math.Max(bl.X, br.X)
            ),
            System.Math.Max(
                System.Math.Max(tl.Y, tr.Y),
                System.Math.Max(bl.Y, br.Y)
            )
        );

        return new Rect
        {
            Offset = p1AABB,
            Size = p2AABB - p1AABB
        };
    }

    public bool PointWithin(Vec2<float> point)
    {
        return point.Within(this);
    }
}