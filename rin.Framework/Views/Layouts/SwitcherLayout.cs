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

    public override ISlot MakeSlot(View widget)
    {
        return new Slot(this)
        {
            Child = widget
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null)
        {
            if (SelectedSlot == slot)
            {
                slot.Child.Offset = 0.0f;
                slot.Child.ComputeSize(Container.GetContentSize());
            }
        }
    }

    public override Vec2<float> Apply(Vec2<float> availableSpace)
    {
        if (SelectedSlot is { } slot)
        {
            OnSlotUpdated(slot);
            slot.Child.Offset = 0.0f;
            return slot.Child.ComputeSize(availableSpace);
        }
        return availableSpace;
    }

    public override Vec2<float> ComputeDesiredContentSize()
    {
        return SelectedSlot?.Child.GetDesiredSize() ?? 0.0f;
    }
}