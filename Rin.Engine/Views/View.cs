using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Animation;
using Rin.Engine.Math;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Enums;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views;

public abstract class View : IDisposable, IAnimatable, IUpdatable
{
    private Matrix4x4? _cachedRelativeTransform = Matrix4x4.Identity;
    private readonly Padding _padding = new();
    private Vector2? _cachedDesiredSize;
    private Vector2 _offset;
    private Vector2 _pivot;
    private Vector2 _size;
    private float _angle = 0.0f;
    private Vector2 _translate = Vector2.Zero;
    private Vector2 _scale = Vector2.One;

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
            _cachedRelativeTransform = null;
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
            _cachedRelativeTransform = null;
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
            _cachedRelativeTransform = null;
        }
    }

    /// <summary>
    ///     The translation of this view in parent space
    /// </summary>
    public Vector2 Translate
    {
        get => new(_translate.X, _translate.Y);
        set
        { 
            _translate.X = value.X; 
            _translate.Y = value.Y;
            _cachedRelativeTransform = null;
        }
    }

    /// <summary>
    ///     The scale of this view in parent space
    /// </summary>
    public Vector2 Scale
    {
        get => new(_scale.X, _scale.Y);
        set
        {
            _scale.X = value.X;
            _scale.Y = value.Y;
            _cachedRelativeTransform = null;
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
        get => _angle;
        set
        {
            _angle = value;
            _cachedRelativeTransform = null;
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


    public virtual void Dispose()
    {
    }

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
    public Vector2 ComputeSize(in Vector2 availableSpace, bool fill = false)
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
    protected abstract Vector2 LayoutContent(in Vector2 availableSpace);

    /// <summary>
    ///     Computes the relative/local transformation matrix for this view
    /// </summary>
    /// <returns></returns>
    public Matrix4x4 ComputeLocalTransform()
    {
        if (_cachedRelativeTransform is { } cached) return cached;

        var rotation = Matrix4x4.Identity.Scale(Scale).Rotate2dDegrees(Angle).Translate(Size * Pivot * -1.0f);
        var transform = Matrix4x4.Identity.Translate(Offset + Translate) * rotation;

        _cachedRelativeTransform = transform;
        return transform;
    }

    public Matrix4x4 ComputeAbsoluteTransform()
    {
        var parentTransform = Parent?.ComputeAbsoluteTransform() ?? Matrix4x4.Identity;
        return ComputeLocalTransform() * parentTransform;
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

    public virtual void HandleEvent(SurfaceEvent e, in Matrix4x4 transform)
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
                if (IsSelfHitTestable && OnCursorDown(ev)) ev.Target = this;
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

                    if (!ev.Handled) ev.Handled = OnCursorMove(ev);
                }

                break;
            case ScrollSurfaceEvent ev:
                if (IsSelfHitTestable && OnScroll(ev)) ev.Target = this;
                break;
        }
    }

    public virtual bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        return false;
    }

    public virtual void OnCursorUp(CursorUpSurfaceEvent e)
    {
    }

    protected virtual bool OnCursorMove(CursorMoveSurfaceEvent e)
    {
        return false;
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
    /// <param name="commands"></param>
    public abstract void Collect(in Matrix4x4 transform, in Rect clip, CommandList commands);


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
    public Rect ComputeAABB(in Matrix4x4 transform)
    {
        var tl = new Vector2(0.0f);
        var br = tl + Size;
        var tr = new Vector2(br.X, tl.Y);
        var bl = new Vector2(tl.X, br.Y);

        tl = tl.Transform(transform);
        br = br.Transform(transform);
        tr = tr.Transform(transform);
        bl = bl.Transform(transform);

        var p1AABB = new Vector2(
            float.Min(
                float.Min(tl.X, tr.X),
                float.Min(bl.X, br.X)
            ),
            float.Min(
                float.Min(tl.Y, tr.Y),
                float.Min(bl.Y, br.Y)
            )
        );
        var p2AABB = new Vector2(
            float.Max(
                float.Max(tl.X, tr.X),
                float.Max(bl.X, br.X)
            ),
            float.Max(
                float.Max(tl.Y, tr.Y),
                float.Max(bl.Y, br.Y)
            )
        );

        return new Rect
        {
            Offset = p1AABB,
            Size = p2AABB - p1AABB
        };
    }

    public bool PointWithin(in Matrix4x4 transform, in Vector2 point, bool useInverse = false)
    {
        return Rect.PointWithin(Size, transform, point, useInverse);
    }
}