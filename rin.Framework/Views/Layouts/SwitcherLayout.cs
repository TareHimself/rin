using System.Numerics;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

public class SwitcherLayout(CompositeView container) : InfiniteChildrenLayout
{
    private int _selected = 0;
    public override CompositeView Container { get; } = container;

    public int SelectedIndex
    {
        get => _selected;
        set
        {
            var lastSelected = _selected;
            var numSlots = SlotCount;
            _selected = System.Math.Clamp(value, 0, numSlots == 0 ? 0 : numSlots - 1);
            if (lastSelected != _selected) Container.Invalidate(InvalidationType.Layout);
        }
    }

    public ISlot? SelectedSlot
    {
        get
        {
            var slots = GetSlots().ToArray();
            if(slots.Length <= SelectedIndex) return null;
            return slots[SelectedIndex];
        }
    }

    public override void Dispose()
    {
        
    }

    public override ISlot MakeSlot(View view)
    {
        return new Slot(this)
        {
            Child = view
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null)
        {
            if (SelectedSlot == slot)
            {
                slot.Child.Offset = default;
                slot.Child.ComputeSize(Container.GetContentSize());
            }
        }
    }

    public override Vector2 Apply(Vector2 availableSpace)
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