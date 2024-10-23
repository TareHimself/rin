namespace aerox.Runtime.Widgets.Containers;

public class Switcher : Container
{
    private int _selected;

    public Switcher(IEnumerable<Widget> widgets) : base(widgets)
    {
    }
    
    public Switcher()
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
        CheckSize();
        ArrangeSlots(GetContentSize());
    }

    protected override Size2d ComputeContentDesiredSize()
    {
        return SelectedWidget?.GetDesiredSize() ?? new Size2d();
    }

    public override uint GetMaxSlots() => 1;

    public override void OnChildResized(Widget widget)
    {
        if (widget == SelectedWidget) base.OnChildResized(widget);
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
}