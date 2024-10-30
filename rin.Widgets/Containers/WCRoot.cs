namespace rin.Widgets.Containers;

public class WCRoot : Container
{
    protected override Size2d ComputeDesiredContentSize()
    {
        return new Size2d();
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        foreach (var slot in GetSlots())
        {
            OnSlotUpdated(slot);
        }
    }

    public override void OnSlotUpdated(Slot slot)
    {
        base.OnSlotUpdated(slot);
        var widget = slot.GetWidget();
        widget.SetOffset(0.0f);
        widget.SetSize(GetSize());
    }

    public override void SetSize(Size2d size)
    {
        base.SetSize(size);
        ArrangeSlots(GetContentSize());
    }
}