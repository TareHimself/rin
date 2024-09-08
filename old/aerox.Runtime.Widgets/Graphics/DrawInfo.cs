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
    public Rect ClipRect;
    
    /// <summary>
    /// True if the target of this draw info will be occluded (tests are done using an AABB approach
    /// </summary>
    public bool Occluded = false;

    private DrawInfo(List<ClippingRect> clippingRects)
    {
        ClippingRects = clippingRects;
        _target = null;
    }

    public Rect ToAabbRect() => ComputeAxisAlignedBoundingRect(this);

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
                Size = widgetSurface.GetDrawSize(),
            }
        ])
        {
            ClipRect = new Rect()
            {
                Size = widgetSurface.GetDrawSize()
            }
        };
    }

    public static DrawInfo From(Widget widget)
    {
        return new DrawInfo([])
        {
            _target = widget,
            ClipRect = widget.GetRect()
        };
    }

    public DrawInfo AccountFor(Widget widget)
    {
        var clippingRects = ClippingRects.ToList();
        var widgetMat = widget.ComputeRelativeTransform();
        var transformedMat = Transform * widgetMat;
        
        var aabb = ComputeAxisAlignedBoundingRect(new DrawInfo(clippingRects)
        {
            _target = widget,
            Transform = transformedMat
        });
        
        if (widget.ClippingMode == WidgetClippingMode.Bounds)
        {
            clippingRects.Add(new ClippingRect
            {
                Matrix = transformedMat,
                Size = widget.GetDrawSize()
            });
            
            var p1 = aabb.Offset;
            var p2 = aabb.Offset + aabb.Size;
            var mP1 = ClipRect.Offset;
            var mP2 = ClipRect.Offset + ClipRect.Size;

            var clipP1 = new Vector2<float>(System.Math.Max(mP1.X, p1.X), System.Math.Max(mP1.Y, p1.Y));
            var clipP2 = new Vector2<float>(System.Math.Min(mP2.X, p2.X), System.Math.Min(mP2.Y, p2.Y));
            var clip = new Rect(clipP1, clipP2 - clipP1);
            return new DrawInfo(clippingRects)
            {
                _target = widget,
                Transform = transformedMat,
                ClipRect = clip,
                Occluded = !clip.IntersectsWith(aabb)
            };
        }

        return new DrawInfo(clippingRects)
        {
            _target = widget,
            Transform = transformedMat,
            ClipRect = ClipRect,
            Occluded = !ClipRect.IntersectsWith(aabb)
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
        
        var tl = new Vector2<float>(0.0f);
        var br = tl + _target.GetDrawSize();
        var tr = new Vector2<float>(br.X, tl.Y);
        var bl = new Vector2<float>(tl.X, br.Y);

        tl = tl.ApplyTransformation(Transform);
        br = br.ApplyTransformation(Transform);
        tr = tr.ApplyTransformation(Transform);
        bl = bl.ApplyTransformation(Transform);

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

        // Perform AABB test first
        if (!point.Within(new Rect
            {
                Offset = p1AABB,
                Size = p2AABB - p1AABB
            })) return false;

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
        {
            return b >= 0 && c >= 0 && d >= 0;
        }
        else
        {
            return b < 0 && c < 0 && d < 0;
        }
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