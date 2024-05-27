using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Defaults.Containers;

/// <summary>
///     A container that draws children left to right
/// </summary>
public class List(params Widget[] children) : Container(children)
{
    public enum Axis
    {
        Vertical,
        Horizontal
    }

    private Axis _direction = Axis.Vertical;

    public Axis Direction
    {
        get => _direction;
        set
        {
            _direction = value;
            OnDirectionChanged();
        }
    }

    protected virtual void OnDirectionChanged()
    {
        CheckSize();
    }

    public override Size2d ComputeDesiredSize()
    {
        return _direction switch
        {
            Axis.Horizontal => slots.Aggregate(new Size2d(), (size, slot) =>
            {
                var slotSize = slot.GetWidget().GetDesiredSize();
                size.Width += slotSize.Width;
                size.Height = System.Math.Max(size.Height, slotSize.Height);
                return size;
            }),
            Axis.Vertical => slots.Aggregate(new Size2d(), (size, slot) =>
            {
                var slotSize = slot.GetWidget().GetDesiredSize();
                size.Height += slotSize.Height;
                size.Width = System.Math.Max(size.Width, slotSize.Width);
                return size;
            }),
            _ => throw new ArgumentOutOfRangeException()
        };
    }


    protected override void ArrangeSlots(Size2d drawSize)
    {
        switch (_direction)
        {
            case Axis.Horizontal:
                slots.Aggregate(new Vector2<float>(0, 0), (offset, slot) =>
                {
                    var widget = slot.GetWidget();
                    var widgetSize = widget.GetDesiredSize();
                    widget.SetRelativeOffset(offset.Clone());
                    widget.SetDrawSize(new Size2d
                    {
                        Width = widgetSize.Width,
                        Height = widgetSize.Height
                    });
                    offset.X += widgetSize.Width;
                    return offset;
                });
                break;
            case Axis.Vertical:
                slots.Aggregate(new Vector2<float>(0, 0), (offset, slot) =>
                {
                    var widget = slot.GetWidget();
                    widget.SetRelativeOffset(offset.Clone());
                    var widgetSize = widget.GetDesiredSize();
                    widget.SetDrawSize(new Size2d
                    {
                        Height = widgetSize.Height,
                        Width = widgetSize.Width
                    });
                    offset.Y += widgetSize.Height;
                    return offset;
                });
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override uint GetMaxSlots()
    {
        return 0;
    }

    public override void Draw(WidgetFrame frame, DrawInfo info)
    {
        if (slots.Count > 2) frame.AddRect(info.Transform, GetDrawSize(), color: Color.Red);
        foreach (var slot in slots.ToArray())
        {
            var slotDrawInfo = info.AccountFor(slot.GetWidget());
            if (!slotDrawInfo.IntersectsWith(info)) continue;
            slot.GetWidget().Draw(frame, info.AccountFor(slot.GetWidget()));
        }
    }
}