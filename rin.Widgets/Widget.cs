using System.Runtime.InteropServices;
using rin.Core;
using rin.Core.Math;
using rin.Widgets.Events;
using rin.Widgets.Graphics;

namespace rin.Widgets;

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

    private Surface? _cursorUpRoot;
    private Vector2<float> _offset = 0.0f;
    private Size2d _size = new();
    private SWidgetsModule _subsystem = SRuntime.Get().GetModule<SWidgetsModule>();

    /// <summary>
    ///     The local angle to render this widget at
    /// </summary>
    public float Angle = 0.0f;

    

    /// <summary>
    ///     The pivot used to render this widget. Affects <see cref="Angle" /> and <see cref="Scale" />.
    /// </summary>
    public Vector2<float> Pivot = 0.0f;

    /// <summary>
    ///     The local scale to apply to this widget
    /// </summary>
    public Vector2<float> Scale = 1.0f;

    public bool Hovered { get; private set; }
    public Surface? Surface { get; private set; }

    public Container? Parent { get; private set; }

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
            TryUpdateDesiredSize();
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
    public Matrix3 ComputeRelativeTransform() =>
        Matrix3.Identity.Translate(GetOffset()).RotateDeg(Angle).Translate(GetSize() * Pivot - 1.0f);


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
        return new Rect(_offset, _size);
    }

    public Rect GetRect(Vector2<float> offset)
    {
        return new Rect(offset + _offset, _size);
    }

    public void SetParent(Container? widget)
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

    public virtual void ReceiveCursorEnter(CursorMoveEvent e, TransformInfo info, List<Widget> items)
    {
        if (!IsSelfHitTestable()) return;
        items.Add(this);
        if (Hovered) return;

        Hovered = true;
        OnCursorEnter(e);
    }

    public virtual bool ReceiveCursorMove(CursorMoveEvent e, TransformInfo info)
    {
        if (IsSelfHitTestable() && OnCursorMove(e)) return true;

        return false;
    }

    public virtual bool ReceiveScroll(ScrollEvent e, TransformInfo info)
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

    public virtual void ReceiveCursorLeave(CursorMoveEvent e)
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
        return _offset;
    }

    public void SetOffset(Vector2<float> offset)
    {
        _offset = offset;
    }

    public Size2d GetContentSize()
    {
        return _size - new Vector2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }

    public virtual void SetSize(Size2d size)
    {
        _size = size;
    }
    
    public Size2d GetSize()
    {
        return _size;
    }

    public Size2d GetDesiredSize()
    {
        return _cachedDesiredSize ??= ComputeDesiredSize();
    }
    
    public Size2d GetDesiredContentSize()
    {
        return GetDesiredSize() - new Vector2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
    }
    
    protected abstract Size2d ComputeDesiredContentSize();

    private Size2d ComputeDesiredSize() =>
        ComputeDesiredContentSize() + new Vector2<float>(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);

    
    /// <summary>
    /// Attempts to update the desired size of this widget, will inform the parent container if successful
    /// </summary>
    /// <returns>true if the desired size was changed</returns>
    protected virtual bool TryUpdateDesiredSize()
    {
        var newSize = ComputeDesiredSize();

        if (newSize.Equals(_cachedDesiredSize)) return false;

        _cachedDesiredSize = newSize;
        
        Parent?.OnSlotUpdated(this);

        return true;
    }
    
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
        
        return new TransformInfo(newTransform * widget.ComputeRelativeTransform(),widget.GetSize(),info.Depth + 1);
    }
    
    
    /// <summary>
    /// Collect draw commands from this widget
    /// </summary>
    /// <param name="info"></param>
    /// <param name="drawCommands"></param>
    public virtual void Collect(TransformInfo info,DrawCommands drawCommands)
    {
        if (Visibility is WidgetVisibility.Hidden or WidgetVisibility.Collapsed)
        {
            return;
        }
        
        CollectContent(new TransformInfo(info.Transform.Translate(new Vector2<float>(Padding.Left,Padding.Top)),GetContentSize(),info.Depth),drawCommands);
    }
    
    /// <summary>
    /// Collect Draw commands from this widget while accounting for padding offsets
    /// </summary>
    /// <param name="info"></param>
    /// <param name="drawCommands"></param>
    public abstract void CollectContent(TransformInfo info,DrawCommands drawCommands);
}