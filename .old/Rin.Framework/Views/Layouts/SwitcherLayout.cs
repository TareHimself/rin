using System.Numerics;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Enums;

namespace Rin.Framework.Views.Layouts;

public class SwitcherLayout(ICompositeView container) : InfiniteChildrenLayout
{
    private int _selected;
    public override ICompositeView Container { get; } = container;

    public int SelectedIndex
    {
        get => _selected;
        set
        {
            var lastSelected = _selected;
            var numSlots = SlotCount;
            _selected = int.Clamp(value, 0, numSlots == 0 ? 0 : numSlots - 1);
            if (lastSelected != _selected) Container.Invalidate(InvalidationType.Layout);
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
                slot.Child.ComputeSize(Container.GetContentSize());
            }
    }

    public override Vector2 Apply(in Vector2 availableSpace)
    {
        if (SelectedSlot is { } slot)
        {
            OnSlotUpdated(slot);
            slot.Child.Offset = default;
            return slot.Child.ComputeSize(availableSpace);
        }

        return availableSpace;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return SelectedSlot?.Child.GetDesiredSize() ?? new Vector2(0, 0);
    }
}