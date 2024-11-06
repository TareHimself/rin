using rin.Core.Math;

namespace rin.Widgets.Containers;


/// <summary>
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class Root : Container
{
    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return new Vector2<float>();
    }

    protected override void ArrangeSlots(Vector2<float> drawSize)
    {
        foreach (var slot in GetSlots())
        {
            OnSlotUpdated(slot);
        }
    }

    public override void OnSlotUpdated(ContainerSlot slot)
    {
        base.OnSlotUpdated(slot);
        var widget = slot.Child;
        widget.Offset = (0.0f);
        widget.Size = Size;
    }
}