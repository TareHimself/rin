using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Defaults.Containers;

/// <summary>
///     Needs work, do not use
/// </summary>
public class WrapBox : Container
{
    public WrapBox(params Widget[] children) : base(children)
    {
    }

    protected override Size2d ComputeDesiredSize()
    {
        return Slots.Aggregate(new Size2d(), (size, slot) =>
        {
            var slotSize = slot.GetWidget().GetDesiredSize();
            size.Width += slotSize.Width;
            size.Height = System.Math.Max(size.Height, slotSize.Height);
            return size;
        });
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        foreach (var slot in Slots.ToArray())
        {
            var slotInfo = info.AccountFor(this);
            slot.GetWidget().Collect(frame, slotInfo);
        }
    }

    public override uint GetMaxSlots()
    {
        return 0;
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        var offset = new Vector2<float>(0.0f);
        var rowHeight = 0.0f;

        foreach (var slot in Slots)
        {
            var widget = slot.GetWidget();
            widget.SetDrawSize(widget.GetDesiredSize());
            var widgetDrawSize = widget.GetDrawSize();
            if (offset.X + widgetDrawSize.Width > drawSize.Width)
                if (offset.X != 0)
                {
                    offset.X = 0.0f;
                    offset.Y += rowHeight;
                }

            widget.SetRelativeOffset(offset.Clone());

            rowHeight = System.Math.Max(rowHeight, widgetDrawSize.Height);

            offset.X += widgetDrawSize.Width;

            if (!(offset.X >= drawSize.Width)) continue;

            offset.X = 0.0f;
            offset.Y += rowHeight;
        }
    }
}