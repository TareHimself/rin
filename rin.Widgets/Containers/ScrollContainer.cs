using rin.Core.Math;
using rin.Widgets.Events;
using rin.Widgets.Graphics;

namespace rin.Widgets.Containers;


/// <summary>
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class ScrollContainer : ListContainer
{
    private float _offset;

    public float MinBarSize = 40.0f;
    private float _mouseDownOffset;
    private Vector2<float> _mouseDownPos = new(0.0f);
    
    
    public override Vector2<float> Size
    {
        set
        {
            base.Size = value;
            ScrollTo(_offset);
        }
    }

    public ScrollContainer() : base()
    {
        Clip = Widgets.Clip.Bounds;
    }

    public float ScrollScale { get; set; } = 10.0f;
    
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

    protected override void ArrangeSlots(Vector2<float> drawSize)
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
        return Axis switch
        {
            Containers.Axis.Row => size.X,
            Containers.Axis.Column => size.Y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public virtual float GetDesiredAxisSize()
    {
        var desiredSize = GetDesiredContentSize();

        return Axis switch
        {
            Containers.Axis.Row => desiredSize.X,
            Containers.Axis.Column => desiredSize.Y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public virtual float GetMaxScroll()
    {
        var desiredSize = GetDesiredContentSize();

        return Axis switch
        {
            Containers.Axis.Row => desiredSize.X - GetContentSize().X,
            Containers.Axis.Column => desiredSize.Y - GetContentSize().Y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public virtual bool IsScrollable()
    {
        return GetMaxScroll() > 0;
    }

    protected override bool OnScroll(ScrollEvent e)
    {
        if (!IsScrollable()) return base.OnScroll(e);

        switch (Axis)
        {
            case Containers.Axis.Column:
            {
                var delta = e.Delta.Y * -1.0f;
                return ScrollBy(delta * ScrollScale);
            }
            case Containers.Axis.Row:
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
            
            var transform = info.Transform.Translate(new Vector2<float>((float)(size.X - 10.0f), drawOffset));
            
            //frame.AddRect(transform, new Vector2<float>(10.0f, barSize), borderRadius: 7.0f, color: Color.White);
        }
    }

    public override TransformInfo OffsetTransformTo(Widget widget, TransformInfo info, bool withPadding = true)
    {
        return base.OffsetTransformTo(widget, new TransformInfo(Axis switch
        {
            Containers.Axis.Row => info.Transform.Translate(new Vector2<float>(-GetScroll(), 0.0f)),
            Containers.Axis.Column => info.Transform.Translate(new Vector2<float>(0.0f, -GetScroll())),
            _ => throw new ArgumentOutOfRangeException()
        }, info.Size, info.Depth),withPadding);
    }
    
    
    protected override bool OnCursorDown(CursorDownEvent e)
    {
        _mouseDownOffset = _offset;
        _mouseDownPos = e.Position.Cast<float>();
        return true;
    }
    
    protected override bool OnCursorMove(CursorMoveEvent e)
    {
        if (IsPendingMouseUp)
        {
            var pos = _mouseDownPos - e.Position.Cast<float>();
    
            return ScrollTo(Axis switch
            {
                Containers.Axis.Row => _mouseDownOffset + pos.X,
                Containers.Axis.Column => _mouseDownOffset + pos.Y,
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