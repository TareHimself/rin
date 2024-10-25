using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Widgets.Graphics;

namespace aerox.Runtime.Widgets.Containers;

public class ScrollableList : List
{
    private float _offset;

    public float MinBarSize = 40.0f;
    //private float mouseDownOffset;
    //private Vector2<float> MouseDownPos = new(0.0f);

    public ScrollableList(IEnumerable<Widget> children) : base(children)
    {
        //Clip = ClipMode.Bounds;
    }

    public ScrollableList() : base([])
    {
        
    }

    public float ScrollScale { get; set; } = 4.0f;
    
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
        var scrollSize = GetMaxScroll();

        if (scrollSize < 0) return false;

        _offset = System.Math.Clamp(offset, 0, scrollSize);
        
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
    
    public virtual float GetAxisSize()
    {
        var size = GetContentSize();
        return Direction switch
        {
            Axis.Horizontal => size.Width,
            Axis.Vertical => size.Height,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public virtual float GetDesiredAxisSize()
    {
        var desiredSize = GetDesiredContentSize();

        return Direction switch
        {
            Axis.Horizontal => desiredSize.Width,
            Axis.Vertical => desiredSize.Height,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public virtual float GetMaxScroll()
    {
        var desiredSize = GetDesiredContentSize();

        return Direction switch
        {
            Axis.Horizontal => desiredSize.Width - GetContentSize().Width,
            Axis.Vertical => desiredSize.Height - GetContentSize().Height,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public virtual bool IsScrollable()
    {
        return GetMaxScroll() > 0;
    }

    public override void SetSize(Size2d size)
    {
        base.SetSize(size);
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
                return ScrollBy(delta * ScrollScale);
            }
            case Axis.Horizontal:
            {
                var delta = e.Delta.X;
                return ScrollBy(delta * ScrollScale);
            }
            default:
                return base.OnScroll(e);
        }
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        base.CollectContent(info, drawCommands);
        if (IsScrollable())
        {
            var scroll = GetScroll();
            var maxScroll = GetMaxScroll();
            var axisSize = GetAxisSize();
            var desiredAxisSize = GetDesiredAxisSize();
                
            var barSize = System.Math.Max(MinBarSize, axisSize - (desiredAxisSize - axisSize));
            var availableDist = axisSize - barSize;
            var drawOffset = (float)(availableDist * (System.Math.Max(scroll,0.0001) / maxScroll));

            var size = GetContentSize();
            
            var transform = info.Transform.Translate(new Vector2<float>((float)(size.Width - 10.0f), drawOffset));
            
            //frame.AddRect(transform, new Vector2<float>(10.0f, barSize), borderRadius: 7.0f, color: Color.White);
        }
    }

    public override TransformInfo OffsetTransformTo(Widget widget, TransformInfo info, bool withPadding = true)
    {
        var transform = base.OffsetTransformTo(widget, info,withPadding);
        transform.Transform = Direction switch
        {
            Axis.Horizontal => info.Transform.Translate(new Vector2<float>(-GetScroll(), 0.0f)),
            Axis.Vertical => info.Transform.Translate(new Vector2<float>(0.0f, -GetScroll())),
            _ => throw new ArgumentOutOfRangeException()
        };

        return transform;
    }

    // protected override bool OnCursorDown(CursorDownEvent e)
    // {
    //     mouseDownOffset = _offset;
    //     MouseDownPos = e.Position.Cast<float>();
    //     return true;
    //     return base.OnCursorDown(e);
    // }
    //
    // protected override bool OnCursorMove(CursorMoveEvent e)
    // {
    //     if (IsPendingMouseUp)
    //     {
    //         var pos = MouseDownPos - e.Position.Cast<float>();
    //
    //         return ScrollTo(Direction switch
    //         {
    //             Axis.Horizontal => mouseDownOffset + pos.X,
    //             Axis.Vertical => mouseDownOffset + pos.Y,
    //             _ => throw new ArgumentOutOfRangeException()
    //         });
    //         // var delta = Direction switch
    //         // {
    //         //     Axis.Horizontal => e.Delta.X,
    //         //     Axis.Vertical => e.Delta.Y * -1.0f,
    //         //     _ => throw new ArgumentOutOfRangeException()
    //         // };
    //
    //         // return ScrollBy((float)delta);
    //     }
    //
    //     return base.OnCursorMove(e);
    // }

    public override void OnCursorUp(CursorUpEvent e)
    {
        base.OnCursorUp(e);
    }
}