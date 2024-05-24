using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Windows;

namespace aerox.Runtime.Widgets.Defaults.Containers;

public class ScrollableList : List
{
    private readonly List _inner;

    private float _offset;
    private Vector2<float> MouseDownPos = new(0.0f);
    private float mouseDownOffset = 0.0f;
    
    public ScrollableList(params Widget[] children) : base()
    {
        ClippingMode = EClippingMode.Bounds;
        _inner = new List(children);
        base.AddChild(_inner);
    }

    public float ScrollScale { get; set; } = 1.0f;

    public override Slot? AddChild(Widget widget)
    {
        return _inner.AddChild(widget);
    }

    public override bool RemoveChild(Widget widget)
    {
        return _inner.RemoveChild(widget);
    }

    public override Slot[] GetSlots()
    {
        return _inner.GetSlots();
    }

    protected override void OnDirectionChanged()
    {
        base.OnDirectionChanged();
        _inner.Direction = Direction;
    }

    public override uint GetMaxSlots()
    {
        return 0;
    }

    public virtual bool ScrollBy(float delta)
    {
        var finalOffset = _offset + delta;
        return ScrollTo(finalOffset);
    }

    public virtual bool ScrollTo(float offset)
    {
        var scrollSize = GetScrollSize();

        if (scrollSize < 0) return false;

        _offset = System.Math.Clamp(offset, 0, scrollSize);

        switch (Direction)
        {
            case Axis.Horizontal:
                _inner.SetRelativeOffset(new Vector2<float>(_offset * -1.0f, 0));
                break;
            case Axis.Vertical:
                _inner.SetRelativeOffset(new Vector2<float>(0, _offset * -1.0f));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return System.Math.Abs(offset - _offset) > 0.001;
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        base.ArrangeSlots(drawSize);
        ScrollTo(_offset);
    }

    public virtual float GetScroll()
    {
        return _offset;
    }

    public virtual float GetScrollSize()
    {
        if (slots.Count == 0) return 0;

        var desiredSize = _inner.GetDrawSize();

        return Direction switch
        {
            Axis.Horizontal => desiredSize.Width - GetDrawSize().Width,
            Axis.Vertical => desiredSize.Height - GetDrawSize().Height,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public virtual bool IsScrollable()
    {
        return GetScrollSize() > 0;
    }

    public override void SetDrawSize(Size2d size)
    {
        base.SetDrawSize(size);
        ScrollTo(_offset);
    }

    protected override bool OnScroll(ScrollEvent e)
    {
        if (!IsScrollable()) return base.OnScroll(e);

        switch (Direction)
        {
            case Axis.Vertical:
            {
                var delta = e.Delta.Y * -1.0f;
                return ScrollBy((float)delta * ScrollScale);
            }
            case Axis.Horizontal:
            {
                var delta = e.Delta.X;
                return ScrollBy((float)delta * ScrollScale);
            }
            default:
                return base.OnScroll(e);
        }
    }

    protected override bool OnCursorDown(CursorDownEvent e)
    {
        mouseDownOffset = _offset;
        MouseDownPos = e.Position.Cast<float>();
        return true;
        return base.OnCursorDown(e);
    }

    protected override bool OnCursorMove(CursorMoveEvent e)
    {
        if (IsPendingMouseUp)
        {
            var pos = MouseDownPos - e.Position.Cast<float>();

            return ScrollTo(Direction switch
            {
                Axis.Horizontal => mouseDownOffset + pos.X,
                Axis.Vertical => mouseDownOffset + pos.Y,
                _ => throw new ArgumentOutOfRangeException()
            });
            // var delta = Direction switch
            // {
            //     Axis.Horizontal => e.Delta.X,
            //     Axis.Vertical => e.Delta.Y * -1.0f,
            //     _ => throw new ArgumentOutOfRangeException()
            // };

            // return ScrollBy((float)delta);
        }

        return base.OnCursorMove(e);
    }

    public override void OnCursorUp(CursorUpEvent e)
    {
        base.OnCursorUp(e);
    }

}