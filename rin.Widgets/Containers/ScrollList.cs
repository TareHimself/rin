using rin.Core.Math;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;

namespace rin.Widgets.Containers;

/// <summary>
///     Slot = <see cref="ContainerSlot" />
/// </summary>
public class ScrollList : List
{
    private float _mouseDownOffset;
    private Vector2<float> _mouseDownPos = new(0.0f);
    private float _offset;

    public float MinBarSize = 40.0f;

    public ScrollList()
    {
        Clip = Clip.Bounds;
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

        _offset = Math.Clamp(offset, 0, scrollSize);

        return Math.Abs(offset - _offset) > 0.001;
    }

    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        var newSize = base.ArrangeContent(availableSpace);
        ScrollTo(_offset);
        return newSize;
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
            Axis.Row => size.X,
            Axis.Column => size.Y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public virtual float GetDesiredAxisSize()
    {
        var desiredSize = GetDesiredContentSize();

        return Axis switch
        {
            Axis.Row => desiredSize.X,
            Axis.Column => desiredSize.Y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public virtual float GetMaxScroll()
    {
        var desiredSize = GetDesiredContentSize();

        return Axis switch
        {
            Axis.Row => desiredSize.X - GetContentSize().X,
            Axis.Column => desiredSize.Y - GetContentSize().Y,
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
            case Axis.Column:
            {
                var delta = e.Delta.Y * -1.0f;
                return ScrollBy(delta * ScrollScale);
            }
            case Axis.Row:
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

            var barSize = Math.Max(MinBarSize, axisSize - (desiredAxisSize - axisSize));
            var availableDist = axisSize - barSize;
            var drawOffset = (float)(availableDist * (Math.Max(scroll, 0.0001) / maxScroll));

            var size = GetContentSize();

            var transform = info.Transform.Translate(new Vector2<float>(size.X - 10.0f, drawOffset));
            drawCommands.AddRect(transform, new Vector2<float>(10.0f, barSize), color: Color.White, borderRadius: 7.0f);
            //frame.AddRect(transform, new Vector2<float>(10.0f, barSize), borderRadius: 7.0f, tint: Color.White);
        }
    }

    public override TransformInfo OffsetTransformTo(Widget widget, TransformInfo info, bool withPadding = true)
    {
        return base.OffsetTransformTo(widget, new TransformInfo(Axis switch
        {
            Axis.Row => info.Transform.Translate(new Vector2<float>(-GetScroll(), 0.0f)),
            Axis.Column => info.Transform.Translate(new Vector2<float>(0.0f, -GetScroll())),
            _ => throw new ArgumentOutOfRangeException()
        }, info.Size, info.Depth), withPadding);
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
                Axis.Row => _mouseDownOffset + pos.X,
                Axis.Column => _mouseDownOffset + pos.Y,
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
}