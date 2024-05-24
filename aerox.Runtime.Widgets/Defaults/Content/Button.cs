using aerox.Runtime.Widgets.Defaults.Containers;

namespace aerox.Runtime.Widgets.Defaults.Content;

public class Button : Container
{
    public Button() : base()
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

    public override void Draw(WidgetFrame frame, DrawInfo info)
    {
        DrawSelf(frame,info);
        foreach (var slot in slots)
        {
            slot.GetWidget().Draw(frame,info.AccountFor(slot.GetWidget()));
        }
    }

    protected virtual void DrawSelf(WidgetFrame frame, DrawInfo myInfo)
    {
        
    }

    public override uint GetMaxSlots() => 1;

    protected override void ArrangeSlots(Size2d drawSize)
    {
        foreach (var slot in slots)
        {
            slot.GetWidget().SetRelativeOffset(new (0.0f));
            slot.GetWidget().SetDrawSize(drawSize);
        }
    }
}