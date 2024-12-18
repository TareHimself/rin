using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Composite;

/// <summary>
/// Slot = <see cref="CompositeViewSlot"/>
/// </summary>
public class Switcher : CompositeView
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

    public View? SelectedWidget => GetSlot(SelectedIndex)?.Child;

    public void SelectedWidgetUpdated()
    {
        Invalidate(InvalidationType.Layout);
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return SelectedWidget?.GetDesiredSize() ?? new Vector2<float>();
    }
    

    public override void OnSlotInvalidated(CompositeViewSlot slot, InvalidationType invalidation)
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

    public override IEnumerable<CompositeViewSlot> GetCollectableSlots() => GetSlot(SelectedIndex) is { } slot ? [slot] : [];

    public override IEnumerable<CompositeViewSlot> GetHitTestableSlots() => GetSlot(SelectedIndex) is { } slot ? [slot] : [];
    
}