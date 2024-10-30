using rin.Core.Math;
using rin.Widgets.Events;
using rin.Widgets.Graphics;

namespace rin.Widgets.Content;

public class Button : Container
{
    public Button()
    {
        
    }

    public Button(Widget child) : base([child])
    {
        
    }

    protected override Size2d ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            return slot.GetWidget().GetDesiredSize();
        }
        return new Size2d();
    }

    protected virtual void CollectSelf(TransformInfo info, DrawCommands drawCommands)
    {
    }

    public override void Collect(TransformInfo info, DrawCommands drawCommands)
    {
        if (Visibility is not WidgetVisibility.Hidden or WidgetVisibility.Collapsed)
        {
            CollectSelf(info,drawCommands);
        }
        base.Collect(info, drawCommands);
    }

    public override int GetMaxSlots() => 1;

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