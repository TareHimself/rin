using System.Numerics;
using Rin.Core.Views.Composite;

namespace Rin.Core.Views.Layouts;


public class SwitcherLayout(ICompositeView container) : InfiniteChildrenLayout
{
    public override ICompositeView Container { get; } = container;

    private ISlot[] _selectedSlots = [];
    private ISlot[]? _selectedSource;
    private int _selectedIndex;

    public int SelectedIndex
    {
        get;
        set
        {
            var lastSelected = field;
            var numSlots = SlotCount;
            field = int.Clamp(value, 0, numSlots == 0 ? 0 : numSlots - 1);
            if (lastSelected != field)
            {
                Container.InvalidateDesiredSize();
                Container.InvalidateLayout();
            }
        }
    }

    public ISlot? SelectedSlot
    {
        get
        {
            var slots = GetSlots();
            return SelectedIndex < slots.Length ? slots[SelectedIndex] : null;
        }
    }

    public ISlot[] GetSelectedSlots()
    {
        var slots = GetSlots();
        if (!ReferenceEquals(slots, _selectedSource) || SelectedIndex != _selectedIndex)
        {
            _selectedSource = slots;
            _selectedIndex = SelectedIndex;
            _selectedSlots = SelectedSlot is { } slot ? [slot] : [];
        }

        return _selectedSlots;
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
        {
            if (SelectedSlot == slot)
            {
                slot.Child.Offset = default;
                slot.Child.Layout(Container.GetContentSize());
            }
        }
            
    }

    public override Vector2 Apply(in Vector2 availableSpace)
    {
        if (SelectedSlot is { } slot)
        {
            slot.Child.Offset = default;
            slot.Child.Layout(Container.GetContentSize());
            return slot.Child.Layout(availableSpace);
        }

        return availableSpace;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return SelectedSlot?.Child.GetDesiredSize() ?? new Vector2(0, 0);
    }
    
}