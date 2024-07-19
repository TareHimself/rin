using System.Runtime.InteropServices;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;

namespace aerox.Runtime.Widgets;

// [StructLayout(LayoutKind.Sequential)]
// public struct WidgetPushConstants
// {
//     public Vector4<float> clip;
//
//     public Vector4<float> extent;
// }
[StructLayout(LayoutKind.Sequential)]
public struct WidgetPushConstants
{
    public Matrix3 Transform;

    public Vector2<float> Size;
}

public abstract class Widget : Disposable
{
    private Size2d? _cachedDesiredSize;

    private WidgetSurface? _cursorUpRoot;
    private Vector2<float> _relativeOffset = 0.0f;
    private Size2d _relativeSize = new();
    private SWidgetsModule _subsystem = SRuntime.Get().GetModule<SWidgetsModule>();

    /// <summary>
    ///     The local angle to render this widget at
    /// </summary>
    public float Angle = 0.0f;

    public WidgetClippingMode ClippingMode = WidgetClippingMode.None;

    /// <summary>
    ///     The pivot used to render this widget. Affects <see cref="Angle" /> and <see cref="Scale" />.
    /// </summary>
    public Vector2<float> Pivot = 0.0f;

    /// <summary>
    ///     The local scale to apply to this widget
    /// </summary>
    public Vector2<float> Scale = 1.0f;

    public bool Hovered { get; private set; }
    public WidgetSurface? Surface { get; private set; }

    public ContainerBase? Parent { get; private set; }

    public WidgetVisibility Visibility { get; set; } = WidgetVisibility.Visible;

    private WidgetPadding _padding = new WidgetPadding();
    /// <summary>
    ///     The Padding For This Widget (Left, Top, Right, Bottom)
    /// </summary>
    public WidgetPadding Padding
    {
        get => _padding;
        set
        {
            _padding = value;
            CheckSize();
        }
    }


    protected bool IsPendingMouseUp => _cursorUpRoot is { Disposed: false };
    
    /// <summary>
    /// Check if this widget is focused by its current surface
    /// </summary>
    public bool Focused => Surface?.FocusedWidget == this;


    /// <summary>
    ///     Computes the relative/local transformation matrix for this widget
    /// </summary>
    /// <returns></returns>
    public Matrix3 ComputeRelativeTransform()
    {
        var m = Matrix3.Identity;
        var offset = _relativeOffset + new Vector2<float>(Padding.Left,Padding.Top);
        
        m = m.Translate(offset + _relativeSize * Pivot);
        m = m.RotateDeg(Angle);
        m = m.Scale(Scale);
        m = m.Translate(offset * Pivot * -1.0f);
        return m;
    }


    public Matrix3 ComputeAbsoluteTransform()
    {
        var parentTransform = Parent?.ComputeAbsoluteTransform() ?? Matrix3.Identity;
        return ComputeRelativeTransform() * parentTransform;
    }

    public void SetVisibility(WidgetVisibility visibility)
    {
        Visibility = visibility;
    }

    public Rect GetRect()
    {
        return new Rect(_relativeOffset, _relativeSize);
    }

    public Rect GetRect(Vector2<float> offset)
    {
        return new Rect(offset + _relativeOffset, _relativeSize);
    }

    public void SetParent(ContainerBase? widget)
    {
        Parent = widget;
    }

    public bool IsSelfHitTestable()
    {
        return Visibility is WidgetVisibility.Visible or WidgetVisibility.VisibleNoHitTestSelf;
    }

    public bool IsChildrenHitTestable()
    {
        return Visibility is WidgetVisibility.Visible or WidgetVisibility.VisibleNoHitTestChildren;
    }

    public bool IsHitTestable()
    {
        return IsSelfHitTestable() || IsChildrenHitTestable();
    }


    public virtual void NotifyAddedToSurface(WidgetSurface widgetSurface)
    {
        Surface = widgetSurface;
        OnAddedToSurface(widgetSurface);
    }

    public virtual void NotifyRemovedFromSurface(WidgetSurface widgetSurface)
    {
        if (Surface == widgetSurface) Surface = null;
        OnRemovedFromSurface(widgetSurface);
    }

    protected virtual void OnAddedToSurface(WidgetSurface widgetSurface)
    {
    }

    protected virtual void OnRemovedFromSurface(WidgetSurface widgetSurface)
    {
    }


    public virtual Widget? ReceiveCursorDown(CursorDownEvent e, DrawInfo info)
    {
        if (IsSelfHitTestable())
            if (OnCursorDown(e))
            {
                _cursorUpRoot = Surface;
                BindCursorUp();
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

    public virtual void ReceiveCursorEnter(CursorMoveEvent e, DrawInfo info, List<Widget> items)
    {
        if (!IsSelfHitTestable()) return;
        items.Add(this);
        if (Hovered) return;

        Hovered = true;
        OnCursorEnter(e);
    }

    public virtual bool ReceiveCursorMove(CursorMoveEvent e, DrawInfo info)
    {
        if (IsSelfHitTestable() && OnCursorMove(e)) return true;

        return false;
    }

    public virtual bool ReceiveScroll(ScrollEvent e, DrawInfo info)
    {
        return IsSelfHitTestable() && OnScroll(e);
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

    public virtual void ReceiveCursorLeave(CursorMoveEvent e, DrawInfo info)
    {
        if (!Hovered) return;

        Hovered = false;
        OnCursorLeave(e);
    }

    protected virtual void OnCursorLeave(CursorMoveEvent e)
    {
    }

    protected virtual bool OnScroll(ScrollEvent e)
    {
        return false;
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

    public Vector2<float> GetOffset()
    {
        return _relativeOffset;
    }

    public void SetRelativeOffset(Vector2<float> offset)
    {
        _relativeOffset = offset;
    }

    public Size2d GetDrawSize()
    {
        return _relativeSize - new Vector2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }

    public virtual void SetDrawSize(Size2d size)
    {
        _relativeSize = size;
    }

    public Size2d GetDesiredSize()
    {
        return _cachedDesiredSize ?? ComputeFinalDesiredSize();
    }

    protected abstract Size2d ComputeDesiredSize();

    private Size2d ComputeFinalDesiredSize() =>
        ComputeDesiredSize() + new Vector2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);

    protected virtual bool CheckSize()
    {
        var newSize = ComputeFinalDesiredSize();

        if (newSize.Equals(_cachedDesiredSize)) return false;

        _cachedDesiredSize = newSize;
        Parent?.OnChildResized(this);

        return true;
    }


    public abstract void Collect(WidgetFrame frame, DrawInfo info);
}