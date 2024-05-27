using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets;

public struct ClippingRect
{
    public Matrix3 Matrix;
    public Size2d Size;
}

public class DrawInfo : ICloneable<DrawInfo>
{
    public readonly List<ClippingRect> ClippingRects;
    private Widget? _target;
    public Matrix3 Transform = Matrix3.Identity;

    private DrawInfo(List<ClippingRect> clippingRects)
    {
        ClippingRects = clippingRects;
        _target = null;
    }

    public DrawInfo Clone()
    {
        return new DrawInfo(ClippingRects)
        {
            _target = _target
        };
    }


    public static DrawInfo From(WidgetSurface widgetSurface)
    {
        return new DrawInfo([
            new ClippingRect
            {
                Matrix = Matrix3.Identity,
                Size = widgetSurface.GetDrawSize()
            }
        ]);
    }

    public static DrawInfo From(Widget widget)
    {
        return new DrawInfo([])
        {
            _target = widget
        };
    }

    public DrawInfo AccountFor(Widget widget)
    {
        var clippingRects = ClippingRects.ToList();
        var widgetMat = widget.ComputeRelativeTransform();
        var transformedMat = Transform * widgetMat;

        if (widget.ClippingMode == EClippingMode.Bounds)
            clippingRects.Add(new ClippingRect
            {
                Matrix = transformedMat,
                Size = widget.GetDrawSize()
            });

        return new DrawInfo(clippingRects)
        {
            _target = widget,
            Transform = transformedMat
        };
    }

    //public static Vector2<float>[] ClippingRectToPoints()

    public static Rect ComputeAxisAlignedBoundingRect(DrawInfo target)
    {
        if (target._target == null) return new Rect();

        var tl = new Vector2<float>(0.0f);
        var br = tl + target._target.GetDrawSize();
        var tr = new Vector2<float>(br.X, tl.Y);
        var bl = new Vector2<float>(tl.X, br.Y);

        tl = tl.ApplyTransformation(target.Transform);
        br = br.ApplyTransformation(target.Transform);
        tr = tr.ApplyTransformation(target.Transform);
        bl = bl.ApplyTransformation(target.Transform);

        var p1AABB = new Vector2<float>(
            System.Math.Min(
                System.Math.Min(tl.X, tr.X),
                System.Math.Min(bl.X, br.X)
            ),
            System.Math.Min(
                System.Math.Min(tl.Y, tr.Y),
                System.Math.Min(bl.Y, br.Y)
            )
        );
        var p2AABB = new Vector2<float>(
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

    public bool IntersectsWith(DrawInfo target)
    {
        var targetAARect = ComputeAxisAlignedBoundingRect(target);
        var myAARect = ComputeAxisAlignedBoundingRect(this);
        return targetAARect.IntersectsWith(myAARect);
    }

    public bool PointWithin(Vector2<float> point)
    {
        if (_target == null) return false;

        return point.Within(ComputeAxisAlignedBoundingRect(this));
        // //return point.Within(Clip);
        // var tl = new Vector2<float>(0.0f);
        // var br = (tl + _target.GetDrawSize());
        // var tr = new Vector2<float>(br.X, tl.Y);
        // var bl = new Vector2<float>(tl.X, br.Y);
        //
        // tl = tl.ApplyTransformation(Transform);
        // br = br.ApplyTransformation(Transform);
        // tr = tr.ApplyTransformation(Transform);
        // bl = bl.ApplyTransformation(Transform);
        //
        // var p1AABB = new Vector2<float>(
        //     System.Math.Min(
        //         System.Math.Min(tl.X, tr.X), 
        //         System.Math.Min(bl.X, br.X)
        //         ),
        //     System.Math.Min(
        //         System.Math.Min(tl.Y, tr.Y), 
        //         System.Math.Min(bl.Y, br.Y)
        //         )
        //     );
        // var p2AABB = new Vector2<float>(
        //     System.Math.Max(
        //         System.Math.Max(tl.X, tr.X), 
        //         System.Math.Max(bl.X, br.X)
        //     ),
        //     System.Math.Max(
        //         System.Math.Max(tl.Y, tr.Y), 
        //         System.Math.Max(bl.Y, br.Y)
        //     )
        // );
        //
        // // Check axis-aligned bounding box
        // return point.Within(p1AABB, p2AABB);

        // var total = point.Acosd(tl) + point.Acosd(tr) + point.Acosd(br) +
        //             point.Acosd(bl);
        // if ((360.0f - total) < 0.1)
        // {
        //     Console.WriteLine("Intersection");
        // }
        // return true;
    }


    public static implicit operator WidgetPushConstants(DrawInfo info)
    {
        return new WidgetPushConstants()
        {
            Transform = info.Transform,
            Size = info._target?.GetDrawSize() ?? new Size2d()
        };
    }
}