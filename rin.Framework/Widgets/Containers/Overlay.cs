using rin.Framework.Core.Math;
using rin.Framework.Core.Extensions;
using rin.Framework.Widgets.Events;

namespace rin.Framework.Widgets.Containers;

/// <summary>
/// A container that draws children on top of each other
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class Overlay : ContainerWidget
{
    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return GetSlots().Aggregate(new Vector2<float>(), (size, slot) =>
        {
            var slotSize = slot.Child.GetDesiredSize();
            size.Y = System.Math.Max(size.Y, slotSize.Y);
            size.X = System.Math.Max(size.X, slotSize.X);
            return size;
        });
    }


    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        var dims = new Vector2<float>(0.0f);
        
        // First pass is for widgets with content to figure out their size
        foreach (var slot in GetSlots())
        {
            var desiredSize = slot.Child.GetDesiredSize();
            if(desiredSize.X <= 0.0f || desiredSize.Y <= 0.0f) continue;
            var widgetSize = slot.Child.ComputeSize(availableSpace);
            
            dims.X = Math.Max(dims.X, widgetSize.X.FiniteOr());
            dims.Y = Math.Max(dims.Y, widgetSize.Y.FiniteOr());
        }
        
        // Second pass is for widgets that adapt to the size of the container
        foreach (var slot in GetSlots())
        {
            slot.Child.Offset = (new Vector2<float>(0, 0));
            slot.Child.ComputeSize(dims);
        }
        return new Vector2<float>(Math.Min(dims.X,availableSpace.X),Math.Min(dims.Y,availableSpace.Y));
    }

    public override IEnumerable<ContainerSlot> GetHitTestableSlots() => base.GetHitTestableSlots().AsReversed();

    // protected override bool ChildrenReceiveScroll(ScrollEvent e, TransformInfo info)
    // {
    //     var position = e.Position.Cast<float>();
    //     var myInfo = OffsetTransformTo()
    //     foreach (var slot in GetSlots().AsReversed())
    //         if (myInfo.AccountFor(slot.GetWidget()).PointWithin(position))
    //             return slot.GetWidget().ReceiveScroll(e, myInfo);
    //
    //     return false;
    // }
    //
    // protected override void ChildrenReceiveCursorEnter(CursorMoveEvent e, TransformInfo info, List<Widget> items)
    // {
    //     var myInfo = info.AccountFor(this);
    //     var position = e.Position.Cast<float>();
    //     foreach (var slot in _slot.AsReversed())
    //         if (myInfo.AccountFor(slot.GetWidget()).PointWithin(position))
    //         {
    //             slot.GetWidget().ReceiveCursorEnter(e, myInfo, items);
    //             break;
    //         }
    // }
    //
    // protected override Widget? ChildrenReceiveCursorDown(CursorDownEvent e, TransformInfo info)
    // {
    //     var position = e.Position.Cast<float>();
    //     var myInfo = info.AccountFor(this);
    //     foreach (var slot in _slot.AsReversed())
    //         if (myInfo.AccountFor(slot.GetWidget()).PointWithin(position))
    //             return slot.GetWidget().ReceiveCursorDown(e, myInfo);
    //
    //     return null;
    // }
    //
    // protected override bool ChildrenReceiveCursorMove(CursorMoveEvent e, TransformInfo info)
    // {
    //     var position = e.Position.Cast<float>();
    //     foreach (var slot in _slot.AsReversed())
    //     {
    //         var slotInfo = info.AccountFor(slot.GetWidget());
    //         if (slotInfo.PointWithin(position))
    //             return slot.GetWidget().ReceiveCursorMove(e, slotInfo);
    //     }
    //
    //     return false;
    // }
}