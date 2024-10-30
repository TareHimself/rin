using rin.Widgets.Graphics;

namespace rin.Widgets.Containers;

public class WCSwitcher : Container
{
    private int _selected;

    public WCSwitcher(IEnumerable<Widget> widgets) : base(widgets)
    {
    }
    
    public WCSwitcher()
    {
    }

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

    public Widget? SelectedWidget => GetSlot(SelectedIndex)?.GetWidget();

    public void SelectedWidgetUpdated()
    {
        TryUpdateDesiredSize();
        ArrangeSlots(GetContentSize());
    }

    protected override Size2d ComputeDesiredContentSize()
    {
        return SelectedWidget?.GetDesiredSize() ?? new Size2d();
    }
    
    public override void OnSlotUpdated(Slot slot)
    {
        if (slot.GetWidget() == SelectedWidget)
        {
            base.OnSlotUpdated(slot);
        }
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        if (GetSlot(SelectedIndex) is { } slot)
        {
            var widget = slot.GetWidget();

            if (widget.GetContentSize().Equals(drawSize)) return;

            widget.SetOffset(0.0f);
            widget.SetSize(drawSize);
        }
    }

    public override IEnumerable<Slot> GetCollectableSlots() => GetSlot(SelectedIndex) is { } slot ? [slot] : [];

    public override IEnumerable<Slot> GetHitTestableSlots() => GetSlot(SelectedIndex) is { } slot ? [slot] : [];
    
}