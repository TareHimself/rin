using System.Numerics;
using Rin.Framework.Views.Composite;

namespace Rin.Framework.Views.Layouts;

public class SwitcherLayout(ICompositeView container) : InfiniteChildrenLayout
{
    public override ICompositeView Container { get; } = container;

    public int SelectedIndex
    {
        get;
        set
        {
            var lastSelected = field;
            var numSlots = SlotCount;
            field = int.Clamp(value, 0, numSlots == 0 ? 0 : numSlots - 1);
            if (lastSelected != field) Container.InvalidateLayout();
        }
    }

    public ISlot? SelectedSlot
    {
        get
        {
            var slots = GetSlots().ToArray();
            if (slots.Length <= SelectedIndex) return null;
            return slots[SelectedIndex];
        }
    }

    public override ISlot MakeSlot(IView view)
    {
        return new Slot(this)
        {
            Child = view
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null)
            if (SelectedSlot == slot)
            {
                slot.Child.Offset = default;
                slot.Child.Layout(Container.GetContentSize());
            }
    }

    public override Vector2 Apply(in Vector2 availableSpace)
    {
        if (SelectedSlot is { } slot)
        {
            OnSlotUpdated(slot);
            slot.Child.Offset = default;
            return slot.Child.Layout(availableSpace);
        }

        return availableSpace;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return SelectedSlot?.Child.GetDesiredSize() ?? new Vector2(0, 0);
    }
}