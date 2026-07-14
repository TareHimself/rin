using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Shared.Math;

namespace Rin.Core.Graphics;

[NoReorder]
public record struct Rect2D
{
    public Vector2 Offset;
    public Vector2 Size;

    public Rect2D()
    {
    }

    public Rect2D(in Vector2 offset, in Vector2 size)
    {
        Offset = offset;
        Size = size;
    }

    /// <summary>
    ///     Creates a rect that is the AABB of the transformed rect
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="size"></param>
    /// <param name="transform"></param>
    public Rect2D(in Vector2 offset, in Vector2 size, in Matrix4x4 transform)
    {
        var tl = offset;
        var br = tl + size;
        var tr = new Vector2(br.X, tl.Y);
        var bl = new Vector2(tl.X, br.Y);

        tl = tl.Transform(transform);
        br = br.Transform(transform);
        tr = tr.Transform(transform);
        bl = bl.Transform(transform);

        var p1 = new Vector2(
            float.Min(
                float.Min(tl.X, tr.X),
                float.Min(bl.X, br.X)
            ),
            float.Min(
                float.Min(tl.Y, tr.Y),
                float.Min(bl.Y, br.Y)
            )
        );
        var p2 = new Vector2(
            float.Max(
                float.Max(tl.X, tr.X),
                float.Max(bl.X, br.X)
            ),
            float.Max(
                float.Max(tl.Y, tr.Y),
                float.Max(bl.Y, br.Y)
            )
        );

        Offset = p1;
        Size = p2 - p1;
    }

    public static implicit operator Vector4(Rect2D rect)
    {
        return new Vector4(rect.Offset.X, rect.Offset.Y, rect.Size.X,
            rect.Size.X);
    }

    public static bool IntersectsWith(Rect2D a, Rect2D b)
    {
        var a1 = a.Offset;
        var a2 = a1 + a.Size;
        var b1 = b.Offset;
        var b2 = b1 + b.Size;

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
    ///     Clamps a rect to the specified area
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="area"></param>
    /// <returns></returns>
    public static Rect2D Clamp(Rect2D rect, Rect2D area)
    {
        if (!IntersectsWith(rect, area)) return new Rect2D(area.Offset, new Vector2());

        var a1 = rect.Offset;
        var a2 = a1 + rect.Size;
        var b1 = area.Offset;
        var b2 = b1 + area.Size;

        var offset = new Vector2(float.Max(a1.X, b1.X), float.Max(a1.Y, b1.Y));

        var p2 = new Vector2(float.Min(a2.X, b2.X), float.Min(a2.Y, b2.Y));

        return new Rect2D(offset, p2 - offset);
    }

    public static implicit operator Pair<Vector2, Vector2>(Rect2D rect)
    {
        return new Pair<Vector2, Vector2>(rect.Offset, rect.Offset + rect.Size);
    }

    public static bool PointWithin(in Vector2 size, in Matrix4x4 transform, in Vector2 point, bool useInverse = true)
    {
        var tl = Vector2.Zero;
        var br = tl + size;
        var tr = new Vector2(br.X, tl.Y);
        var bl = new Vector2(tl.X, br.Y);

        if (useInverse)
        {
            var transformedPoint = point.Transform(transform.Inverse());

            return transformedPoint.Within(Vector2.Zero, size);
        }
        // var transformedPoint = point.ApplyTransformation(transform.Inverse());
        //
        // return transformedPoint.Within(Vector2.Zero, Size);

        tl = tl.Transform(transform);
        br = br.Transform(transform);
        tr = tr.Transform(transform);
        bl = bl.Transform(transform);

        var p1 = new Vector2(
            float.Min(
                float.Min(tl.X, tr.X),
                float.Min(bl.X, br.X)
            ),
            float.Min(
                float.Min(tl.Y, tr.Y),
                float.Min(bl.Y, br.Y)
            )
        );
        var p2 = new Vector2(
            float.Max(
                float.Max(tl.X, tr.X),
                float.Max(bl.X, br.X)
            ),
            float.Max(
                float.Max(tl.Y, tr.Y),
                float.Max(bl.Y, br.Y)
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
    
    public readonly void Deconstruct(out Vector2 offset, out Vector2 size)
    {
        offset = Offset;
        size = Size;
    }
}