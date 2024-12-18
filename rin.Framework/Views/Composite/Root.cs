using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Composite;


/// <summary>
/// Slot = <see cref="CompositeViewSlot"/>
/// </summary>
public class Root : CompositeView
{
    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return new Vector2<float>();
    }


    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        foreach (var slot in GetSlots())
        {
            var widget = slot.Child;
            widget.Offset = 0.0f;
            widget.ComputeSize(availableSpace);
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
    

    public override void OnSlotInvalidated(CompositeViewSlot slot, InvalidationType invalidation)
    {
        var widget = slot.Child;
        widget.Offset = 0.0f;
        widget.ComputeSize(GetContentSize());
    }
}