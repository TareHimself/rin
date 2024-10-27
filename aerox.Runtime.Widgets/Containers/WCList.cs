using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Containers;

/// <summary>
///     A container that draws children left to right
/// </summary>
public class WCList(IEnumerable<Widget> children) : Container(children)
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
        TryUpdateDesiredSize();
    }

    protected override Size2d ComputeDesiredContentSize()
    {
        return _direction switch
        {
            Axis.Horizontal => GetSlots().Aggregate(new Size2d(), (size, slot) =>
            {
                var slotSize = slot.GetWidget().GetDesiredSize();
                size.Width += slotSize.Width;
                size.Height = System.Math.Max(size.Height, slotSize.Height);
                return size;
            }),
            Axis.Vertical => GetSlots().Aggregate(new Size2d(), (size, slot) =>
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
        var offset = new Vector2<float>(0.0f);
        switch (_direction)
        {
            case Axis.Horizontal:
            {
                foreach (var slot in GetSlots())
                {
                    var widget = slot.GetWidget();
                    var widgetSize = widget.GetDesiredSize();
                    widget.SetOffset(offset.Clone());
                    widget.SetSize(new Size2d
                    {
                        Width = widgetSize.Width,
                        Height = widgetSize.Height
                    });
                    offset.X += widgetSize.Width;
                }
                break;
            }
            case Axis.Vertical:
                
                {
                    foreach (var slot in GetSlots())
                    {
                        var widget = slot.GetWidget();
                        widget.SetOffset(offset.Clone());
                        var widgetSize = widget.GetDesiredSize();
                        widget.SetSize(new Size2d
                        {
                            Height = widgetSize.Height,
                            Width = widgetSize.Width
                        });
                        offset.Y += widgetSize.Height;
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void OnSlotUpdated(Slot slot)
    {
        TryUpdateDesiredSize();
        ArrangeSlots(GetContentSize());
    }
}