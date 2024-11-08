using rin.Core;
using rin.Core.Animation;
using rin.Core.Math;
using rin.Widgets.Enums;
using rin.Widgets.Events;
using rin.Widgets.Graphics;

namespace rin.Widgets;

public abstract class Widget : Disposable, IAnimatable
{
    private Vector2<float>? _cachedDesiredSize;
    private Surface? _cursorUpRoot;

    private Transform2d _transform = new();
    private Vector2<float> _offset = 0.0f;
    private Vector2<float> _size = 0.0f;
    private Vector2<float> _pivot = 0.0f;
    private readonly Padding _padding = new();
    
    /// <summary>
    /// The offset of this widget in parent space
    /// </summary>
    public Vector2<float> Offset
    {
        get => new Vector2<float>(_offset.X, _offset.Y);
        set
        {
            _offset.X = value.X;
            _offset.Y = value.Y;
        }
    }

    /// <summary>
    /// The size of this widget in parent space
    /// </summary>
    public Vector2<float> Size
    {
        get => new Vector2<float>(_size.X, _size.Y);
        set
        {
            _size.X = value.X;
            _size.Y = value.Y;
        }
    }
    
    /// <summary>
    /// The pivot used to render this widget. Affects <see cref="Angle" /> and <see cref="Scale" />.
    /// </summary>
    public Vector2<float> Pivot
    {
        get => new Vector2<float>(_pivot.X, _pivot.Y);
        set
        {
            _pivot.X = value.X;
            _pivot.Y = value.Y;
        }
    }
    
    /// <summary>
    /// The translation of this widget in parent space
    /// </summary>
    public Vector2<float> Translate
    {
        get => new Vector2<float>(_transform.Translate.X, _transform.Translate.Y);
        set
        {
            _transform.Translate.X = value.X;
            _transform.Translate.Y = value.Y;
        }
    }
    
    /// <summary>
    /// The scale of this widget in parent space
    /// </summary>
    public Vector2<float> Scale
    {
        get => new Vector2<float>(_transform.Scale.X, _transform.Scale.Y);
        set
        {
            _transform.Scale.X = value.X;
            _transform.Scale.Y = value.Y;
        }
    }
    
    /// <summary>
    ///     The Padding For This Widget (Left, Top, Right, Bottom)
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
    public virtual float Angle { get => _transform.Angle; set => _transform.Angle = value; }
    
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
    
    /// <summary>
    /// The surface this widget is currently on
    /// </summary>
    public Surface? Surface { get; private set; }

    /// <summary>
    /// The parent of this widget
    /// </summary>
    public Container? Parent { get; private set; }
    
    protected bool IsPendingMouseUp => _cursorUpRoot is { Disposed: false };
    
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
    public Vector2<float> ComputeSize(Vector2<float> availableSpace, bool fill = false)
    {
        var padding = new Vector2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
        var contentSize = LayoutContent(availableSpace - padding) + padding;
        return Size = fill ? availableSpace : contentSize;
    }
    
    /// <summary>
    /// Lay's out content in the available space and returns the size taken by the content
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <returns></returns>
    protected abstract Vector2<float> LayoutContent(Vector2<float> availableSpace);
    
    /// <summary>
    ///     Computes the relative/local transformation matrix for this widget
    /// </summary>
    /// <returns></returns>
    public Matrix3 ComputeRelativeTransform() =>
        Matrix3.Identity.Translate(Offset + Translate).RotateDeg(Angle).Scale(Scale).Translate(Size * Pivot - 1.0f);
    
    public Matrix3 ComputeAbsoluteTransform()
    {
        var parentTransform = Parent?.ComputeAbsoluteTransform() ?? Matrix3.Identity;
        return ComputeRelativeTransform() * parentTransform;
    }

    public void SetParent(Container? widget)
    {
        Parent = widget;
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


    public virtual Widget? ReceiveCursorDown(CursorDownEvent e, TransformInfo info)
    {
        if (IsSelfHitTestable)
            if (OnCursorDown(e))
            {
                _cursorUpRoot = Surface;
                BindCursorUp();
                if (IsFocusable)
                {
                    Surface?.RequestFocus(this);
                }
                return this;
            }

        return null;
    }

    protected void BindCursorUp()
    {
        var root = _cursorUpRoot;
        if (root?.Disposed == false) root.OnCursorUp += ReceiveCursorUp;
    }

    protected void UnBindCursorUp()
    {
        var root = _cursorUpRoot;
        if (root?.Disposed == false) root.OnCursorUp -= ReceiveCursorUp;

        _cursorUpRoot = null;
    }

    public virtual void ReceiveCursorUp(CursorUpEvent e)
    {
        UnBindCursorUp();
        OnCursorUp(e);
    }

    public virtual void ReceiveCursorEnter(CursorMoveEvent e, TransformInfo info, List<Widget> items)
    {
        if (!IsSelfHitTestable) return;
        items.Add(this);
        if (IsHovered) return;

        IsHovered = true;
        OnCursorEnter(e);
    }

    public virtual bool ReceiveCursorMove(CursorMoveEvent e, TransformInfo info)
    {
        if (IsSelfHitTestable && OnCursorMove(e)) return true;

        return false;
    }

    public virtual bool ReceiveScroll(ScrollEvent e, TransformInfo info)
    {
        return IsSelfHitTestable && OnScroll(e);
    }

    protected virtual bool OnCursorDown(CursorDownEvent e)
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

    public virtual void ReceiveCursorLeave(CursorMoveEvent e)
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
        UnBindCursorUp();
    }

    public Vector2<float> GetContentSize()
    {
        return _size - new Vector2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }

    public Vector2<float> GetDesiredSize()
    {
        if (Surface == null) return ComputeDesiredSize();
        
        return _cachedDesiredSize ??= ComputeDesiredSize();
    }
    
    public Vector2<float> GetDesiredContentSize()
    {
        return GetDesiredSize() - new Vector2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }
    
    protected abstract Vector2<float> ComputeDesiredContentSize();

    private Vector2<float> ComputeDesiredSize() =>
        ComputeDesiredContentSize() + new Vector2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    
    /// <summary>
    /// Computes the transform info of content 
    /// </summary>
    /// <param name="widget">The content</param>
    /// <param name="info">The Absolute Transform info of this widget</param>
    /// <param name="withPadding">Should we also account for padding ? (should be true except when used in <see cref="CollectContent"/>)</param>
    /// <returns>The Absolute Transform info of content</returns>
    public virtual TransformInfo OffsetTransformTo(Widget widget, TransformInfo info, bool withPadding = true)
    {
        var newTransform = withPadding
            ? info.Transform.Translate(new Vector2<float>(Padding.Left, Padding.Top))
            : info.Transform;
        
        return new TransformInfo(newTransform * widget.ComputeRelativeTransform(),widget.Size,info.Depth + 1);
    }
    
    
    /// <summary>
    /// Collect draw commands from this widget
    /// </summary>
    /// <param name="info"></param>
    /// <param name="drawCommands"></param>
    public virtual void Collect(TransformInfo info,DrawCommands drawCommands)
    {
        if (Visibility is Visibility.Hidden or Visibility.Collapsed)
        {
            return;
        }

        ((IAnimatable)this).Update();
        
        CollectSelf(new TransformInfo(info.Transform,Size,info.Depth),drawCommands);
        
        CollectContent(new TransformInfo(info.Transform.Translate(new Vector2<float>(Padding.Left,Padding.Top)),GetContentSize(),info.Depth),drawCommands);
    }

    protected virtual void CollectSelf(TransformInfo info, DrawCommands drawCommands)
    {
        
    }


    protected virtual void Invalidate(InvalidationType type)
    {
        if(Surface == null) return;
        
        switch (type)
        {
            case InvalidationType.DesiredSize:
            {
                if (_cachedDesiredSize is { } asCachedSize)
                {
                    var newSize = ComputeDesiredSize();

                    if (newSize.Equals(_cachedDesiredSize)) return;
                    _cachedDesiredSize = newSize;
        
                    Parent?.OnChildInvalidated(this,type);
                }
                else
                {
                    _cachedDesiredSize = ComputeDesiredSize();
                    Parent?.OnChildInvalidated(this,type);
                }
            }
                break;
            case InvalidationType.Layout:
                Parent?.OnChildInvalidated(this,type);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    
    /// <summary>
    /// Collect Draw commands from this widget while accounting for padding offsets
    /// </summary>
    /// <param name="info"></param>
    /// <param name="drawCommands"></param>
    public abstract void CollectContent(TransformInfo info,DrawCommands drawCommands);

    public AnimationRunner AnimationRunner { get; init; } = new AnimationRunner();
}