using rin.Core.Math;

namespace rin.Widgets.Containers;

/// <summary>
/// Needs work, do not use
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class WrapBox : Container
{

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return GetSlots().Aggregate(new Vector2<float>(), (size, slot) =>
        {
            var slotSize = slot.Child.GetDesiredSize();
            size.X += slotSize.X;
            size.Y = System.Math.Max(size.Y, slotSize.Y);
            return size;
        });
    }

    protected override void ArrangeSlots(Vector2<float> drawSize)
    {
        var offset = new Vector2<float>(0.0f);
        var rowHeight = 0.0f;

        foreach (var slot in GetSlots())
        {
            var widget = slot.Child;
            widget.Size = (widget.GetDesiredSize());
            var widgetDrawSize = widget.GetContentSize();
            if (offset.X + widgetDrawSize.X > drawSize.X)
                if (offset.X != 0)
                {
                    offset.X = 0.0f;
                    offset.Y += rowHeight;
                }

            widget.Offset = (offset.Clone());

            rowHeight = System.Math.Max(rowHeight, widgetDrawSize.Y);

            offset.X += widgetDrawSize.X;

            if (!(offset.X >= drawSize.X)) continue;

            offset.X = 0.0f;
            offset.Y += rowHeight;
        }
    }
}