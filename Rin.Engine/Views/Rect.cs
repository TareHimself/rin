using System.Numerics;
using Rin.Engine.Core;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views;

public class Rect(Vector2 inOffset, Vector2 inSize) : ICloneable<Rect>
{
    public Vector2 Offset = inOffset;
    public Vector2 Size = inSize;

    public Rect() : this(new Vector2(0, 0), new Vector2())
    {
    }

    public Rect Clone()
    {
        return new Rect(Offset.Clone(), Size.Clone());
    }

    public static implicit operator Vector4(Rect rect)
    {
        return new Vector4(rect.Offset.X, rect.Offset.Y, rect.Size.X,
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
            Size = new Vector2();
            return this;
        }

        var a1 = Offset;
        var a2 = a1 + Size;
        var b1 = area.Offset;
        var b2 = b1 + area.Size;

        Offset = new Vector2(Math.Max(a1.X, b1.X), Math.Max(a1.Y, b1.Y));

        var p2 = new Vector2(Math.Min(a2.X, b2.X), Math.Min(a2.Y, b2.Y));

        Size = p2 - Offset;

        return this;
    }

    public static implicit operator Pair<Vector2, Vector2>(Rect rect)
    {
        return new Pair<Vector2, Vector2>(rect.Offset, rect.Offset + rect.Size);
    }
    
    public static bool PointWithin(Vector2 size,Mat3 transform, Vector2 point,bool useInverse = false)
    {
        var tl = Vector2.Zero;
        var br = tl + size;
        var tr = new Vector2(br.X, tl.Y);
        var bl = new Vector2(tl.X, br.Y);

        if (useInverse)
        {
            var transformedPoint = point.ApplyTransformation(transform.Inverse());
            
            return transformedPoint.Within(Vector2.Zero, size);
        }
        // var transformedPoint = point.ApplyTransformation(transform.Inverse());
        //
        // return transformedPoint.Within(Vector2.Zero, Size);

        tl = tl.ApplyTransformation(transform);
        br = br.ApplyTransformation(transform);
        tr = tr.ApplyTransformation(transform);
        bl = bl.ApplyTransformation(transform);

        var p1 = new Vector2(
            Math.Min(
                Math.Min(tl.X, tr.X),
                Math.Min(bl.X, br.X)
            ),
            Math.Min(
                Math.Min(tl.Y, tr.Y),
                Math.Min(bl.Y, br.Y)
            )
        );
        var p2 = new Vector2(
            Math.Max(
                Math.Max(tl.X, tr.X),
                Math.Max(bl.X, br.X)
            ),
            Math.Max(
                Math.Max(tl.Y, tr.Y),
                Math.Max(bl.Y, br.Y)
            )
        );

        // Perform AABB test first
        if (!point.Within(p1, p2)) return false;

        var top = tr - tl;
        var right = br - tr;
        var bottom = bl - br;
        var left = tl - bl;
        var pTop = point - tl;
        var pRight = point - tr;
        var pBottom = point - br;
        var pLeft = point - bl;
        var a = top.Acos(pTop);
        var b = right.Cross(pRight);
        var c = bottom.Cross(pBottom);
        var d = left.Cross(pLeft);

        if (a >= 0)
            return b >= 0 && c >= 0 && d >= 0;
        return b < 0 && c < 0 && d < 0;
    }
    
    //
    // // ReSharper disable once InconsistentNaming
    // public static Rect MakeAABB(Vector2 size,Mat3 transform,Vector2? offset)
    // {
    //     var tl = offset.GetValueOrDefault(new Vector2(0.0f));
    //     var br = tl + size;
    //     var tr = new Vector2(br.X, tl.Y);
    //     var bl = new Vector2(tl.X, br.Y);
    //
    //     tl = tl.ApplyTransformation(transform);
    //     br = br.ApplyTransformation(transform);
    //     tr = tr.ApplyTransformation(transform);
    //     bl = bl.ApplyTransformation(transform);
    //
    //     var p1AABB = new Vector2(
    //         System.Math.Min(
    //             System.Math.Min(tl.X, tr.X),
    //             System.Math.Min(bl.X, br.X)
    //         ),
    //         System.Math.Min(
    //             System.Math.Min(tl.Y, tr.Y),
    //             System.Math.Min(bl.Y, br.Y)
    //         )
    //     );
    //     var p2AABB = new Vector2(
    //         System.Math.Max(
    //             System.Math.Max(tl.X, tr.X),
    //             System.Math.Max(bl.X, br.X)
    //         ),
    //         System.Math.Max(
    //             System.Math.Max(tl.Y, tr.Y),
    //             System.Math.Max(bl.Y, br.Y)
    //         )
    //     );
    //
    //     return new Rect
    //     {
    //         Offset = p1AABB,
    //         Size = p2AABB - p1AABB
    //     };
    // }

    // public bool PointWithin(Vector2 point)
    // {
    //     return point.Within(this);
    // }
}