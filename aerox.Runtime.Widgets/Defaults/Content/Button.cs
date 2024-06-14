using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Defaults.Content;

public class Button : Container
{
    public Button()
    {
    }

    public Button(Widget child) : base(child)
    {
    }

    public override Size2d ComputeDesiredSize()
    {
        var slot = slots.Count > 0 ? slots[0] : null;
        return slot == null ? new Size2d() : slot.GetWidget().GetDesiredSize();
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        DrawSelf(frame, info);
        foreach (var slot in slots) slot.GetWidget().Collect(frame, info.AccountFor(slot.GetWidget()));
    }

    protected virtual void DrawSelf(WidgetFrame frame, DrawInfo myInfo)
    {
    }

    public override uint GetMaxSlots()
    {
        return 1;
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        foreach (var slot in slots)
        {
            slot.GetWidget().SetRelativeOffset(new Vector2<float>(0.0f));
            slot.GetWidget().SetDrawSize(drawSize);
        }
    }
}