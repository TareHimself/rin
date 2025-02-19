using System.Numerics;
using rin.Framework.Core;
using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics;

public struct ClippingRect
{
    public Mat3 Matrix;
    public Vector2 Size;
}

public class TransformInfo : ICloneable<TransformInfo>
{
    protected Rect _clipRect;
    public int Depth;

    /// <summary>
    ///     True if the target of this draw info will be occluded (tests are done using an AABB approach
    /// </summary>
    public bool Occluded;

    /// <summary>
    ///     This is only used for intersection estimates
    /// </summary>
    public Vector2 Size;

    public Mat3 Transform;

    public TransformInfo(Mat3 transform, Vector2 size, int depth, bool checkOcclusion = false,
        TransformInfo? parent = null, Clip clip = Clip.None)
    {
        Transform = transform;
        Size = size;
        Depth = depth;
        if (checkOcclusion)
        {
            var AABB = ComputeAxisAlignedBoundingRect(this);
            if (parent != null)
                _clipRect = clip switch
                {
                    Clip.None => parent._clipRect,
                    Clip.Bounds => AABB.Clamp(parent._clipRect),
                    _ => throw new ArgumentOutOfRangeException(nameof(clip), clip, null)
                };
            else
                _clipRect = AABB;

            Occluded = !AABB.IntersectsWith(_clipRect);
        }
        else
        {
            _clipRect = new Rect(new Vector2(float.NegativeInfinity), new Vector2(float.PositiveInfinity));
        }
    }

    public TransformInfo(Surface surface)
    {
        Transform = Mat3.Identity;
        Size = surface.GetDrawSize();
        Depth = 0;
        _clipRect = ComputeAxisAlignedBoundingRect(this);
    }

    public TransformInfo Clone()
    {
        return new TransformInfo(Transform, Size, Depth)
        {
            _clipRect = _clipRect,
            Occluded = Occluded
        };
    }

    public Rect ToRect()
    {
        return ComputeAxisAlignedBoundingRect(this);
    }

    //public static Vector2<float>[] ClippingRectToPoints()


    public static Rect ComputeAxisAlignedBoundingRect(TransformInfo target)
    {
        var tl = new Vector2(0.0f);
        var br = tl + target.Size;
        var tr = new Vector2(br.X, tl.Y);
        var bl = new Vector2(tl.X, br.Y);

        tl = tl.ApplyTransformation(target.Transform);
        br = br.ApplyTransformation(target.Transform);
        tr = tr.ApplyTransformation(target.Transform);
        bl = bl.ApplyTransformation(target.Transform);

        var p1AABB = new Vector2(
            Math.Min(
                Math.Min(tl.X, tr.X),
                Math.Min(bl.X, br.X)
            ),
            Math.Min(
                Math.Min(tl.Y, tr.Y),
                Math.Min(bl.Y, br.Y)
            )
        );
        var p2AABB = new Vector2(
            Math.Max(
                Math.Max(tl.X, tr.X),
                Math.Max(bl.X, br.X)
            ),
            Math.Max(
                Math.Max(tl.Y, tr.Y),
                Math.Max(bl.Y, br.Y)
            )
        );

        return new Rect
        {
            Offset = p1AABB,
            Size = p2AABB - p1AABB
        };
    }

    public bool IntersectsWith(TransformInfo target)
    {
        var targetAARect = ComputeAxisAlignedBoundingRect(target);
        var myAARect = ComputeAxisAlignedBoundingRect(this);
        return targetAARect.IntersectsWith(myAARect);
    }

    // public bool PointWithin(Vector2 point)
    // {
    //     var tl = new Vector2(0.0f);
    //     var br = tl + Size;
    //     var tr = new Vector2(br.X, tl.Y);
    //     var bl = new Vector2(tl.X, br.Y);
    //
    //     tl = tl.ApplyTransformation(Transform);
    //     br = br.ApplyTransformation(Transform);
    //     tr = tr.ApplyTransformation(Transform);
    //     bl = bl.ApplyTransformation(Transform);
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
    //     // Perform AABB test first
    //     if (!point.Within(new Rect
    //         {
    //             Offset = p1AABB,
    //             Size = p2AABB - p1AABB
    //         })) return false;
    //
    //     var top = tr - tl;
    //     var right = br - tr;
    //     var bottom = bl - br;
    //     var left = tl - bl;
    //     var pTop = point - tl;
    //     var pRight = point - tr;
    //     var pBottom = point - br;
    //     var pLeft = point - bl;
    //     
    //     var a = top.Acos(pTop);
    //     var b = right.Cross(pRight);
    //     var c = bottom.Cross(pBottom);
    //     var d = left.Cross(pLeft);
    //
    //     if (a >= 0)
    //     {
    //         return b >= 0 && c >= 0 && d >= 0;
    //     }
    //     else
    //     {
    //         return b < 0 && c < 0 && d < 0;
    //     }
    // }
}