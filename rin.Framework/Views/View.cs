using JetBrains.Annotations;
using rin.Framework.Core;
using rin.Framework.Core.Animation;
using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views;

public abstract partial class View : Disposable, IAnimatable
{
    private Vec2<float>? _cachedDesiredSize;
    private Transform2d _transform = new();
    private Vec2<float> _offset = 0.0f;
    private Vec2<float> _size = 0.0f;
    private Vec2<float> _pivot = 0.0f;
    private readonly Padding _padding = new();
    private readonly Atomic<Mat3?> _cachedRelativeTransform = Mat3.Identity;
    
    /// <summary>
    /// The offset of this widget in parent space
    /// </summary>
    public Vec2<float> Offset
    {
        get => new Vec2<float>(_offset.X, _offset.Y);
        set
        {
            _offset.X = value.X;
            _offset.Y = value.Y;
            _cachedRelativeTransform.Value = null;
        }
    }

    /// <summary>
    /// The size of this widget in parent space
    /// </summary>
    public Vec2<float> Size
    {
        get => new Vec2<float>(_size.X, _size.Y);
        set
        {
            _size.X = value.X;
            _size.Y = value.Y;
            _cachedRelativeTransform.Value = null;
        }
    }
    
    /// <summary>
    /// The pivot used to render this widget. Affects <see cref="Angle" /> and <see cref="Scale" />.
    /// </summary>
    public Vec2<float> Pivot
    {
        get => new Vec2<float>(_pivot.X, _pivot.Y);
        set
        {
            _pivot.X = value.X;
            _pivot.Y = value.Y;
            _cachedRelativeTransform.Value = null;
        }
    }
    
    /// <summary>
    /// The translation of this widget in parent space
    /// </summary>
    public Vec2<float> Translate
    {
        get => new Vec2<float>(_transform.Translate.X, _transform.Translate.Y);
        set
        {
            _transform.Translate.X = value.X;
            _transform.Translate.Y = value.Y;
            _cachedRelativeTransform.Value = null;
        }
    }
    
    /// <summary>
    /// The scale of this widget in parent space
    /// </summary>
    public Vec2<float> Scale
    {
        get => new Vec2<float>(_transform.Scale.X, _transform.Scale.Y);
        set
        {
            _transform.Scale.X = value.X;
            _transform.Scale.Y = value.Y;
            _cachedRelativeTransform.Value = null;
        }
    }
    
    /// <summary>
    ///     The Padding For This View (Left, Top, Right, Bottom)
    /// </summary>
    public Padding Padding
    {
        get => new Padding()
        {
            Top = _padding.Top,
            Bottom = _padding.Bottom,
            Left = _padding.Left,
            Right = _padding.Right
        };
        set
        {
            _padding.Top = value.Top;
            _padding.Bottom = value.Bottom;
            _padding.Left = value.Left;
            _padding.Right = value.Right;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    /// <summary>
    /// The angle this widget is to be rendered at in parent space
    /// </summary>
    public virtual float Angle
    {
        get => _transform.Angle;
        set { _transform.Angle = value; _cachedRelativeTransform.Value = null;}
    }

    /// <summary>
    /// The visibility of this widget
    /// </summary>
    public virtual Visibility Visibility { get; set; } = Visibility.Visible;

    
    /// <summary>
    /// Should this widget be hit tested
    /// </summary>
    public bool IsSelfHitTestable => Visibility is Visibility.Visible or Visibility.VisibleNoHitTestSelf;

    /// <summary>
    /// Should this widget's children be hit tested
    /// </summary>
    public bool IsChildrenHitTestable => Visibility is Visibility.Visible or Visibility.VisibleNoHitTestChildren;

    /// <summary>
    /// Should this widget or its children be hit tested
    /// </summary>
    public bool IsHitTestable => IsSelfHitTestable || IsChildrenHitTestable;

    /// <summary>
    /// The current hovered state of this widget
    /// </summary>
    public bool IsHovered { get; private set; }

    public bool IsVisible => Visibility is not (Visibility.Hidden or Visibility.Collapsed);
    
    /// <summary>
    /// The surface this widget is currently on
    /// </summary>
    public Surface? Surface { get; private set; }

    /// <summary>
    /// The parent of this widget
    /// </summary>
    public CompositeView? Parent { get; private set; }
    
    
    /// <summary>
    /// Check if this widget is focused by its current surface
    /// </summary>
    public bool IsFocused => Surface?.FocusedWidget == this;

    public virtual bool IsFocusable { get; } = false;


    /// <summary>
    /// Compute and set this widgets size based on the space available
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <param name="fill">If true will set <see cref="Size"/> to <see cref="availableSpace"/> irrespective of the space taken by content</param>
    /// <returns></returns>
    public Vec2<float> ComputeSize(Vec2<float> availableSpace, bool fill = false)
    {
        var padding = new Vec2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
        var contentSize = LayoutContent(availableSpace - padding) + padding;
        var sizeResult = (fill ? availableSpace : contentSize).FiniteOr();
        return Size = sizeResult;
    }
    
    /// <summary>
    /// Lay's out content in the available space and returns the size taken by the content
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <returns></returns>
    protected abstract Vec2<float> LayoutContent(Vec2<float> availableSpace);

    /// <summary>
    ///     Computes the relative/local transformation matrix for this widget
    /// </summary>
    /// <returns></returns>
    public Mat3 ComputeRelativeTransform()
    {
        if (_cachedRelativeTransform.Value is { } cached)
        {
            return cached;
        }

        var rotation = Mat3.Identity.Scale(Scale).RotateDeg(Angle).Translate(Size * Pivot * -1.0f);
        var transform = Mat3.Identity.Translate(Offset + Translate) * rotation;
        
        _cachedRelativeTransform.Value = transform;
        return transform;
    }
    
    public Mat3 ComputeAbsoluteTransform()
    {
        var parentTransform = Parent?.ComputeAbsoluteTransform() ?? Mat3.Identity;
        return ComputeRelativeTransform() * parentTransform;
    }

    public void SetParent(CompositeView? widget)
    {
        Parent = widget;
        SetSurface(Parent?.Surface);
    }

    public virtual void SetSurface(Surface? surface)
    {
        if (surface != Surface && Surface != null)
        {
            NotifyRemovedFromSurface(Surface);
        }
        else if(surface != null)
        {
            NotifyAddedToSurface(surface);
        }
    }
    
    public virtual void NotifyAddedToSurface(Surface surface)
    {
        Surface = surface;
        OnAddedToSurface(surface);
    }

    public virtual void NotifyRemovedFromSurface(Surface surface)
    {
        if (Surface == surface) Surface = null;
        OnRemovedFromSurface(surface);
    }

    protected virtual void OnAddedToSurface(Surface surface)
    {
    }

    protected virtual void OnRemovedFromSurface(Surface surface)
    {
    }


    public virtual bool NotifyCursorDown(CursorDownEvent e, Mat3 transform)
    {
        if (IsSelfHitTestable)
            if (OnCursorDown(e))
            {
                e.Target = this;
                return true;
            }

        return false;
    }

    public virtual void NotifyCursorUp(CursorUpEvent e)
    {
        //UnBindCursorUp();
        OnCursorUp(e);
    }

    public virtual void NotifyCursorEnter(CursorMoveEvent e, Mat3 transform, List<View> items)
    {
        if (!IsSelfHitTestable) return;
        items.Add(this);
        if (IsHovered) return;

        IsHovered = true;
        OnCursorEnter(e);
    }

    public virtual bool NotifyCursorMove(CursorMoveEvent e, Mat3 transform)
    {
        if (IsSelfHitTestable && OnCursorMove(e)) return true;

        return false;
    }

    public virtual bool NotifyScroll(ScrollEvent e, Mat3 transform)
    {
        return IsSelfHitTestable && OnScroll(e);
    }

    public virtual bool OnCursorDown(CursorDownEvent e)
    {
        return false;
    }

    public virtual void OnCursorUp(CursorUpEvent e)
    {
    }

    protected virtual bool OnCursorMove(CursorMoveEvent e)
    {
        return false;
    }

    protected virtual void OnCursorEnter(CursorMoveEvent e)
    {
    }

    public virtual void NotifyCursorLeave(CursorMoveEvent e)
    {
        if (!IsHovered) return;

        IsHovered = false;
        OnCursorLeave(e);
    }

    protected virtual void OnCursorLeave(CursorMoveEvent e)
    {
    }

    protected virtual bool OnScroll(ScrollEvent e)
    {
        return false;
    }
    
    public virtual void OnCharacter(CharacterEvent e)
    {
        
    }
    
    public virtual void OnKeyboard(KeyboardEvent e)
    {
        
    }
    
    public virtual void OnFocus()
    {
    }
    
    public virtual void OnFocusLost()
    {
    }

    protected override void OnDispose(bool isManual)
    {
       // UnBindCursorUp();
    }
    [PublicAPI]
    public Vec2<float> GetContentSize()
    {
        return _size - new Vec2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }
    [PublicAPI]
    public Vec2<float> GetDesiredSize()
    {
        if (Surface == null) return ComputeDesiredSize();
        
        return _cachedDesiredSize ??= ComputeDesiredSize();
    }
    [PublicAPI]
    public Vec2<float> GetDesiredContentSize()
    {
        return GetDesiredSize() - new Vec2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }
    
    protected abstract Vec2<float> ComputeDesiredContentSize();

    private Vec2<float> ComputeDesiredSize() =>
        ComputeDesiredContentSize() + new Vec2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);

    /// <summary>
    /// Collect draw commands from this widget
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="clip"></param>
    /// <param name="drawCommands"></param>
    public abstract void Collect(Mat3 transform, Rect clip, DrawCommands drawCommands);


    public virtual bool TryUpdateDesiredSize()
    {
        if (_cachedDesiredSize is { } asCachedSize)
        {
            var newSize = ComputeDesiredSize();

            if (newSize.NearlyEquals(asCachedSize)) return false;
            
            _cachedDesiredSize = newSize;
        }
        else
        {
            _cachedDesiredSize = ComputeDesiredSize();
        }

        return true;
    }
    
    
    public virtual void Invalidate(InvalidationType type)
    {
        if(Surface == null) return;
        
        switch (type)
        {
            case InvalidationType.DesiredSize:
            {
                if (_cachedDesiredSize is { } asCachedSize)
                {
                    var newSize = ComputeDesiredSize();
    
                    if (newSize.NearlyEquals(_cachedDesiredSize.Value)) return;
                    _cachedDesiredSize = newSize;
                }
                else
                {
                    _cachedDesiredSize = ComputeDesiredSize();
                }
    
                Parent?.OnChildInvalidated(this,type);
            }
                break;
            case InvalidationType.Layout:
                Parent?.OnChildInvalidated(this,type);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    

    public AnimationRunner AnimationRunner { get; init; } = new ();

    // ReSharper disable once InconsistentNaming
    public Rect ComputeAABB(Mat3 transform)
    {
        var tl = new Vec2<float>(0.0f);
        var br = tl + Size;
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
    
    public bool PointWithin(Mat3 transform,Vec2<float> point)
    {
        var tl = new Vec2<float>(0.0f);
        var br = tl + Size;
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
}