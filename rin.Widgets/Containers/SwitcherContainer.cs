using rin.Core.Math;
using rin.Widgets.Graphics;

namespace rin.Widgets.Containers;

/// <summary>
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class SwitcherContainer : Container
{
    private int _selected;
    
    public int SelectedIndex
    {
        get => _selected;
        set
        {
            var lastSelected = _selected;
            var numSlots = GetNumSlots();
            _selected = System.Math.Clamp(value, 0, numSlots == 0 ? 0 : numSlots - 1);
            if (lastSelected != _selected) SelectedWidgetUpdated();
        }
    }

    public Widget? SelectedWidget => GetSlot(SelectedIndex)?.Child;

    public void SelectedWidgetUpdated()
    {
        TryUpdateDesiredSize();
        ArrangeSlots(GetContentSize());
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return SelectedWidget?.GetDesiredSize() ?? new Vector2<float>();
    }
    
    public override void OnSlotUpdated(ContainerSlot slot)
    {
        if (slot.Child == SelectedWidget)
        {
            base.OnSlotUpdated(slot);
        }
    }

    protected override void ArrangeSlots(Vector2<float> drawSize)
    {
        if (GetSlot(SelectedIndex) is { } slot)
        {
            var widget = slot.Child;

            if (widget.GetContentSize().Equals(drawSize)) return;

            widget.Offset = (0.0f);
            widget.Size = drawSize;
        }
    }

    public override IEnumerable<ContainerSlot> GetCollectableSlots() => GetSlot(SelectedIndex) is { } slot ? [slot] : [];

    public override IEnumerable<ContainerSlot> GetHitTestableSlots() => GetSlot(SelectedIndex) is { } slot ? [slot] : [];
    
}