using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Containers;

/// <summary>
///     Needs work, do not use
/// </summary>
public class WrapBox : Container
{
    public WrapBox(params Widget[] children) : base(children)
    {
    }

    protected override Size2d ComputeDesiredContentSize()
    {
        return GetSlots().Aggregate(new Size2d(), (size, slot) =>
        {
            var slotSize = slot.GetWidget().GetDesiredSize();
            size.Width += slotSize.Width;
            size.Height = System.Math.Max(size.Height, slotSize.Height);
            return size;
        });
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        var offset = new Vector2<float>(0.0f);
        var rowHeight = 0.0f;

        foreach (var slot in GetSlots())
        {
            var widget = slot.GetWidget();
            widget.SetSize(widget.GetDesiredSize());
            var widgetDrawSize = widget.GetContentSize();
            if (offset.X + widgetDrawSize.Width > drawSize.Width)
                if (offset.X != 0)
                {
                    offset.X = 0.0f;
                    offset.Y += rowHeight;
                }

            widget.SetOffset(offset.Clone());

            rowHeight = System.Math.Max(rowHeight, widgetDrawSize.Height);

            offset.X += widgetDrawSize.Width;

            if (!(offset.X >= drawSize.Width)) continue;

            offset.X = 0.0f;
            offset.Y += rowHeight;
        }
    }
}