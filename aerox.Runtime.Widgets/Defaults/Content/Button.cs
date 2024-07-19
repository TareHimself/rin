using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;

namespace aerox.Runtime.Widgets.Defaults.Content;

public class Button : Container
{
    public Button()
    {
    }

    public Button(Widget child) : base(child)
    {
    }

    protected override Size2d ComputeDesiredSize()
    {
        var slot = Slots.Count > 0 ? Slots[0] : null;
        return slot == null ? new Size2d() : slot.GetWidget().GetDesiredSize();
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        CollectSelf(frame, info);
        foreach (var slot in Slots) slot.GetWidget().Collect(frame, info.AccountFor(slot.GetWidget()));
    }

    protected virtual void CollectSelf(WidgetFrame frame, DrawInfo myInfo)
    {
    }

    public override uint GetMaxSlots()
    {
        return 1;
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        foreach (var slot in Slots)
        {
            slot.GetWidget().SetRelativeOffset(new Vector2<float>(0.0f));
            slot.GetWidget().SetDrawSize(drawSize);
        }
    }

    protected override bool OnCursorDown(CursorDownEvent e)
    {
        return true;
    }
}