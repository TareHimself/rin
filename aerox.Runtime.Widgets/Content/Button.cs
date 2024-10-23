using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Widgets.Graphics;

namespace aerox.Runtime.Widgets.Content;

public class Button : Container
{
    public Button()
    {
        
    }

    public Button(Widget child) : base([child])
    {
        
    }

    protected override Size2d ComputeContentDesiredSize()
    {
        if (GetSlot(0) is { } slot)
        {
            return slot.GetWidget().GetDesiredSize();
        }
        return new Size2d();
    }

    protected virtual void CollectSelf(WidgetFrame frame, TransformInfo myInfo)
    {
    }

    public override uint GetMaxSlots()
    {
        return 1;
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        if (GetSlot(0) is { } slot)
        {
            slot.GetWidget().SetOffset(new Vector2<float>(0.0f));
            slot.GetWidget().SetSize(drawSize);
        }
    }

    protected override bool OnCursorDown(CursorDownEvent e)
    {
        return true;
    }
}