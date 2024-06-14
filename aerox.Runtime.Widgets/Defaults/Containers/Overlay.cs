using aerox.Runtime.Extensions;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;

namespace aerox.Runtime.Widgets.Defaults.Containers;

/// <summary>
///     A container that draws children on top of each other
/// </summary>
public class Overlay : Container
{
    public Overlay(params Widget[] children) : base(children)
    {
    }


    public override Size2d ComputeDesiredSize()
    {
        return slots.Aggregate(new Size2d(), (size, slot) =>
        {
            var slotSize = slot.GetWidget().GetDesiredSize();
            size.Height = System.Math.Max(size.Height, slotSize.Height);
            size.Width = System.Math.Max(size.Width, slotSize.Width);
            return size;
        });
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        var drawInfo = info.AccountFor(this);
        foreach (var slot in slots) slot.GetWidget().Collect(frame, drawInfo);
    }

    public override uint GetMaxSlots()
    {
        return 0;
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        foreach (var slot in slots.ToArray())
        {
            slot.GetWidget().SetRelativeOffset(new Vector2<float>(0, 0));
            slot.GetWidget().SetDrawSize(drawSize);
        }
    }

    protected override bool ChildrenReceiveScroll(ScrollEvent e, DrawInfo info)
    {
        var position = e.Position.Cast<float>();
        var myInfo = info.AccountFor(this);
        foreach (var slot in slots.AsReversed())
            if (myInfo.AccountFor(slot.GetWidget()).PointWithin(position))
                return slot.GetWidget().ReceiveScroll(e, myInfo);

        return false;
    }

    protected override void ChildrenReceiveCursorEnter(CursorMoveEvent e, DrawInfo info, List<Widget> items)
    {
        var myInfo = info.AccountFor(this);
        var position = e.Position.Cast<float>();
        foreach (var slot in slots.AsReversed())
            if (myInfo.AccountFor(slot.GetWidget()).PointWithin(position))
            {
                slot.GetWidget().ReceiveCursorEnter(e, myInfo, items);
                break;
            }
    }

    protected override Widget? ChildrenReceiveCursorDown(CursorDownEvent e, DrawInfo info)
    {
        var position = e.Position.Cast<float>();
        var myInfo = info.AccountFor(this);
        foreach (var slot in slots.AsReversed())
            if (myInfo.AccountFor(slot.GetWidget()).PointWithin(position))
                return slot.GetWidget().ReceiveCursorDown(e, myInfo);

        return null;
    }

    protected override bool ChildrenReceiveCursorMove(CursorMoveEvent e, DrawInfo info)
    {
        var position = e.Position.Cast<float>();
        foreach (var slot in slots.AsReversed())
        {
            var slotInfo = info.AccountFor(slot.GetWidget());
            if (slotInfo.PointWithin(position))
                return slot.GetWidget().ReceiveCursorMove(e, slotInfo);
        }

        return false;
    }
}