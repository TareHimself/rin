using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Core;
using Rin.Engine.Core.Animation;
using Rin.Engine.Core.Math;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Enums;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views;

public abstract class View : IDisposable, IAnimatable, IUpdatable
{
    private readonly Atomic<Mat3?> _cachedRelativeTransform = Mat3.Identity;
    private readonly Padding _padding = new();
    private Vector2? _cachedDesiredSize;
    private Vector2 _offset;
    private Vector2 _pivot;
    private Vector2 _size;
    private Transform2d _transform = new();

    /// <summary>
    ///     The offset of this view in parent space
    /// </summary>
    public Vector2 Offset
    {
        get => new(_offset.X, _offset.Y);
        set
        {
            _offset.X = value.X;
            _offset.Y = value.Y;
            _cachedRelativeTransform.Value = null;
        }
    }

    /// <summary>
    ///     The size of this view in parent space
    /// </summary>
    public Vector2 Size
    {
        get => new(_size.X, _size.Y);
        set
        {
            _size.X = value.X;
            _size.Y = value.Y;
            _cachedRelativeTransform.Value = null;
        }
    }

    /// <summary>
    ///     The pivot used to render this view. Affects <see cref="Angle" /> and <see cref="Scale" />.
    /// </summary>
    public Vector2 Pivot
    {
        get => new(_pivot.X, _pivot.Y);
        set
        {
            _pivot.X = value.X;
            _pivot.Y = value.Y;
            _cachedRelativeTransform.Value = null;
        }
    }

    /// <summary>
    ///     The translation of this view in parent space
    /// </summary>
    public Vector2 Translate
    {
        get => new(_transform.Translate.X, _transform.Translate.Y);
        set
        {
            _transform.Translate.X = value.X;
            _transform.Translate.Y = value.Y;
            _cachedRelativeTransform.Value = null;
        }
    }

    /// <summary>
    ///     The scale of this view in parent space
    /// </summary>
    public Vector2 Scale
    {
        get => new(_transform.Scale.X, _transform.Scale.Y);
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
        get => new()
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
    ///     The angle this view is to be rendered at in parent space
    /// </summary>
    public float Angle
    {
        get => _transform.Angle;
        set
        {
            _transform.Angle = value;
            _cachedRelativeTransform.Value = null;
        }
    }

    /// <summary>
    ///     The visibility of this view
    /// </summary>
    public Visibility Visibility { get; set; } = Visibility.Visible;


    /// <summary>
    ///     Should this view be hit tested
    /// </summary>
    public bool IsSelfHitTestable => Visibility is Visibility.Visible or Visibility.VisibleNoHitTestSelf;

    /// <summary>
    ///     Should this view's children be hit tested
    /// </summary>
    public bool IsChildrenHitTestable => Visibility is Visibility.Visible or Visibility.VisibleNoHitTestChildren;

    /// <summary>
    ///     Should this view or its children be hit tested
    /// </summary>
    public bool IsHitTestable => IsSelfHitTestable || IsChildrenHitTestable;

    /// <summary>
    ///     The current hovered state of this view
    /// </summary>
    public bool IsHovered { get; private set; }

    public bool IsVisible => Visibility is not (Visibility.Hidden or Visibility.Collapsed);

    /// <summary>
    ///     The surface this view is currently on
    /// </summary>
    public Surface? Surface { get; private set; }

    /// <summary>
    ///     The parent of this view
    /// </summary>
    public CompositeView? Parent { get; private set; }


    /// <summary>
    ///     Check if this view is focused by its current surface
    /// </summary>
    public bool IsFocused => Surface?.FocusedView == this;

    public virtual bool IsFocusable { get; } = false;


    public AnimationRunner AnimationRunner { get; init; } = new();

    public virtual void Update(float deltaTime)
    {

    }


    /// <summary>
    ///     Compute and set this views size based on the space available
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <param name="fill">
    ///     If true will set <see cref="Size" /> to <see cref="availableSpace" /> irrespective of the space
    ///     taken by content
    /// </param>
    /// <returns></returns>
    public Vector2 ComputeSize(Vector2 availableSpace, bool fill = false)
    {
        var padding = new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
        var contentSize = LayoutContent(availableSpace - padding) + padding;
        var sizeResult = (fill ? availableSpace : contentSize).FiniteOr();
        return Size = sizeResult;
    }

    /// <summary>
    ///     Lay's out content in the available space and returns the size taken by the content
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <returns></returns>
    protected abstract Vector2 LayoutContent(Vector2 availableSpace);

    /// <summary>
    ///     Computes the relative/local transformation matrix for this view
    /// </summary>
    /// <returns></returns>
    public Mat3 ComputeRelativeTransform()
    {
        if (_cachedRelativeTransform.Value is { } cached) return cached;

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

    public void SetParent(CompositeView? view)
    {
        Parent = view;
        SetSurface(Parent?.Surface);
    }

    public virtual void SetSurface(Surface? surface)
    {
        if (surface != Surface && Surface != null)
            NotifyRemovedFromSurface(Surface);
        else if (surface != null) NotifyAddedToSurface(surface);
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

    public virtual void HandleEvent(SurfaceEvent e, Mat3 transform)
    {
        switch (e)
        {
            case CursorEnterSurfaceEvent ev:
                if (IsSelfHitTestable)
                {
                    ev.Entered.Add(this);
                    if (!IsHovered)
                    {
                        IsHovered = true;
                        OnCursorEnter(ev);
                    }
                }
                break;
            case CursorDownSurfaceEvent ev:
                if (IsSelfHitTestable && OnCursorDown(ev))
                {
                    ev.Target = this;
                }
                break;
            case CursorUpSurfaceEvent ev:
                OnCursorUp(ev);
                break;
            case CursorMoveSurfaceEvent ev:
                if (IsSelfHitTestable)
                {
                    ev.Over.Add(this); 
                    
                    if (!IsHovered)
                    {
                        IsHovered = true;
                        OnCursorEnter(ev);
                    }

                    OnCursorMove(ev);
                }
                break;
            case ScrollSurfaceEvent ev:
                if (IsSelfHitTestable && OnScroll(ev))
                {
                    ev.Target = this;
                }
                break;
        }
    }

    public virtual bool NotifyCursorDown(CursorDownSurfaceEvent e, Mat3 transform)
    {
        if (IsSelfHitTestable)
            if (OnCursorDown(e))
            {
                e.Target = this;
                return true;
            }

        return false;
    }

    public virtual void NotifyCursorUp(CursorUpSurfaceEvent e)
    {
        //UnBindCursorUp();
        OnCursorUp(e);
    }

    public virtual void NotifyCursorEnter(CursorMoveSurfaceEvent e, Mat3 transform, List<View> items)
    {
        if (!IsSelfHitTestable) return;
        items.Add(this);
        if (IsHovered) return;

        IsHovered = true;
        OnCursorEnter(e);
    }

    public virtual bool NotifyCursorMove(CursorMoveSurfaceEvent e, Mat3 transform)
    {
        if (IsSelfHitTestable) OnCursorMove(e);

        return false;
    }

    public virtual bool NotifyScroll(ScrollSurfaceEvent e, Mat3 transform)
    {
        return IsSelfHitTestable && OnScroll(e);
    }

    public virtual bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        return false;
    }

    public virtual void OnCursorUp(CursorUpSurfaceEvent e)
    {
    }

    protected virtual void OnCursorMove(CursorMoveSurfaceEvent e)
    {

    }

    protected virtual void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
    }

    public virtual void NotifyCursorLeave()
    {
        if (!IsHovered) return;

        IsHovered = false;
        OnCursorLeave();
    }

    protected virtual void OnCursorLeave()
    {
    }

    protected virtual bool OnScroll(ScrollSurfaceEvent e)
    {
        return false;
    }

    public virtual void OnCharacter(CharacterSurfaceEvent e)
    {
    }

    public virtual void OnKeyboard(KeyboardSurfaceEvent e)
    {
    }

    public virtual void OnFocus()
    {
    }

    public virtual void OnFocusLost()
    {
    }


    public virtual void Dispose()
    {
        
    }
    
    [PublicAPI]
    public Vector2 GetContentSize()
    {
        return _size - new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }

    [PublicAPI]
    public Vector2 GetDesiredSize()
    {
        if (Surface == null) return ComputeDesiredSize();

        return _cachedDesiredSize ??= ComputeDesiredSize();
    }

    [PublicAPI]
    public Vector2 GetDesiredContentSize()
    {
        return GetDesiredSize() - new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }

    protected abstract Vector2 ComputeDesiredContentSize();

    private Vector2 ComputeDesiredSize()
    {
        return ComputeDesiredContentSize() + new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }

    /// <summary>
    ///     Collect draw commands from this view
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="clip"></param>
    /// <param name="passCommands"></param>
    public abstract void Collect(Mat3 transform, Rect clip, PassCommands passCommands);


    public virtual bool TryUpdateDesiredSize()
    {
        if (_cachedDesiredSize is { } asCachedSize)
        {
            var newSize = ComputeDesiredSize();

            if (newSize == asCachedSize) return false;

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
        if (Surface == null) return;

        switch (type)
        {
            case InvalidationType.DesiredSize:
            {
                if (_cachedDesiredSize is { } asCachedSize)
                {
                    var newSize = ComputeDesiredSize();

                    if (newSize == _cachedDesiredSize.Value) return;
                    _cachedDesiredSize = newSize;
                }
                else
                {
                    _cachedDesiredSize = ComputeDesiredSize();
                }

                Parent?.OnChildInvalidated(this, type);
            }
                break;
            case InvalidationType.Layout:
                Parent?.OnChildInvalidated(this, type);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    // ReSharper disable once InconsistentNaming
    public Rect ComputeAABB(Mat3 transform)
    {
        var tl = new Vector2(0.0f);
        var br = tl + Size;
        var tr = new Vector2(br.X, tl.Y);
        var bl = new Vector2(tl.X, br.Y);

        tl = tl.ApplyTransformation(transform);
        br = br.ApplyTransformation(transform);
        tr = tr.ApplyTransformation(transform);
        bl = bl.ApplyTransformation(transform);

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

    public bool PointWithin(Mat3 transform, Vector2 point,bool useInverse = false)
    {
        return Rect.PointWithin(Size,transform, point, useInverse);
        var tl = new Vector2(0.0f);
        var br = tl + Size;
        var tr = new Vector2(br.X, tl.Y);
        var bl = new Vector2(tl.X, br.Y);

        if (useInverse)
        {
            var transformedPoint = point.ApplyTransformation(transform.Inverse());
            
            return transformedPoint.Within(Vector2.Zero, Size);
        }
        // var transformedPoint = point.ApplyTransformation(transform.Inverse());
        //
        // return transformedPoint.Within(Vector2.Zero, Size);

        tl = tl.ApplyTransformation(transform);
        br = br.ApplyTransformation(transform);
        tr = tr.ApplyTransformation(transform);
        bl = bl.ApplyTransformation(transform);

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

        // Perform AABB test first
        if (!point.Within(p1AABB, p2AABB)) return false;

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
}