using rin.Core.Math;
using rin.Widgets.Enums;
using rin.Widgets.Graphics;

namespace rin.Widgets.Containers;

/// <summary>
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class Switcher : ContainerWidget
{
    private int _selected;
    
    public int SelectedIndex
    {
        get => _selected;
        set
        {
            var lastSelected = _selected;
            var numSlots = GetSlotsCount();
            _selected = System.Math.Clamp(value, 0, numSlots == 0 ? 0 : numSlots - 1);
            if (lastSelected != _selected) SelectedWidgetUpdated();
        }
    }

    public Widget? SelectedWidget => GetSlot(SelectedIndex)?.Child;

    public void SelectedWidgetUpdated()
    {
        Invalidate(InvalidationType.Layout);
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return SelectedWidget?.GetDesiredSize() ?? new Vector2<float>();
    }
    

    public override void OnSlotInvalidated(ContainerSlot slot, InvalidationType invalidation)
    {
        if (slot.Child == SelectedWidget)
        {
            base.OnSlotInvalidated(slot, invalidation);
        }
    }

    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        if (GetSlot(SelectedIndex) is { } slot)
        {
            var widget = slot.Child;

            if (widget.Size.Equals(availableSpace)) return widget.Size;

            widget.Offset = (0.0f);
            return widget.ComputeSize(availableSpace);
        }

        return 0.0f;
    }

    public override IEnumerable<ContainerSlot> GetCollectableSlots() => GetSlot(SelectedIndex) is { } slot ? [slot] : [];

    public override IEnumerable<ContainerSlot> GetHitTestableSlots() => GetSlot(SelectedIndex) is { } slot ? [slot] : [];
    
}