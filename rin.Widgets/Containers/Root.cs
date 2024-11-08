using rin.Core.Math;
using rin.Widgets.Enums;

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


    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        foreach (var slot in GetSlots())
        {
            OnSlotUpdated(slot);
        }

        return availableSpace;
    }

    // protected override void ArrangeContent(Vector2<float> drawSize)
    // {
    //     foreach (var slot in GetSlots())
    //     {
    //         OnSlotUpdated(slot);
    //     }
    // }

    public override void OnSlotUpdated(ContainerSlot slot)
    {
        base.OnSlotUpdated(slot);
        var widget = slot.Child;
        widget.Offset = (0.0f);
        widget.ComputeSize(GetContentSize());
    }

    public override void OnSlotInvalidated(ContainerSlot slot, InvalidationType invalidation)
    {
        var widget = slot.Child;
        widget.Offset = 0.0f;
        widget.ComputeSize(GetContentSize());
    }
}