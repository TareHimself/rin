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

    public EClippingMode ClippingMode = EClippingMode.None;

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

    public EVisibility Visibility { get; set; } = EVisibility.Visible;


    protected bool IsPendingMouseUp => _cursorUpRoot != null && !_cursorUpRoot.Disposed;
    
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
        m = m.Translate(_relativeOffset + _relativeSize * Pivot);
        m = m.RotateDeg(Angle);
        m = m.Scale(Scale);
        m = m.Translate(_relativeSize * Pivot * -1.0f);
        return m;
    }

    public void SetVisibility(EVisibility visibility)
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
        return Visibility is EVisibility.Visible or EVisibility.VisibleNoHitTestSelf;
    }

    public bool IsChildrenHitTestable()
    {
        return Visibility is EVisibility.Visible or EVisibility.VisibleNoHitTestChildren;
    }

    public bool IsHitTestable()
    {
        return IsSelfHitTestable() || IsChildrenHitTestable();
    }


    public virtual void NotifyAddedToRoot(WidgetSurface widgetSurface)
    {
        Surface = widgetSurface;
        OnAddedToRoot(widgetSurface);
    }

    public virtual void NotifyRemovedFromRoot(WidgetSurface widgetSurface)
    {
        if (Surface == widgetSurface) Surface = null;
        OnRemovedFromRoot(widgetSurface);
    }

    protected virtual void OnAddedToRoot(WidgetSurface widgetSurface)
    {
    }

    protected virtual void OnRemovedFromRoot(WidgetSurface widgetSurface)
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
        return _relativeSize;
    }

    public virtual void SetDrawSize(Size2d size)
    {
        _relativeSize = size;
    }

    public Size2d GetDesiredSize()
    {
        if (_cachedDesiredSize != null) return _cachedDesiredSize;
        _cachedDesiredSize = ComputeDesiredSize();
        return _cachedDesiredSize;
    }

    public abstract Size2d ComputeDesiredSize();

    protected virtual bool CheckSize()
    {
        var newSize = ComputeDesiredSize();

        if (newSize.Equals(_cachedDesiredSize)) return false;

        _cachedDesiredSize = newSize;
        Parent?.OnChildResized(this);

        return true;
    }


    public abstract void Draw(WidgetFrame frame, DrawInfo info);
}