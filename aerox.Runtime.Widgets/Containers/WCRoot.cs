namespace aerox.Runtime.Widgets.Containers;

public class WCRoot : Container
{
    protected override Size2d ComputeContentDesiredSize()
    {
        return new Size2d();
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        foreach (var slot in GetSlots())
        {
            var widget = slot.GetWidget();
            widget.SetOffset(0.0f);
            widget.SetSize(drawSize);
        }
    }

    public override void SetSize(Size2d size)
    {
        base.SetSize(size);
        ArrangeSlots(GetContentSize());
    }
}