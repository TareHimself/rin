namespace aerox.Runtime.Widgets.Defaults.Containers;

public class Switcher : Container
{
    private int _selected;

    public Switcher(params Widget[] widgets) : base(widgets)
    {
    }

    public int SelectedIndex
    {
        get => _selected;
        set
        {
            var lastSelected = _selected;
            _selected = System.Math.Clamp(value, 0, Slots.Count == 0 ? 0 : Slots.Count - 1);
            if (lastSelected != _selected) SelectedWidgetUpdated();
        }
    }

    public Widget? SelectedWidget => Slots.Count > SelectedIndex ? Slots[SelectedIndex].GetWidget() : null;

    public void SelectedWidgetUpdated()
    {
        CheckSize();
        ArrangeSlots(GetDrawSize());
    }

    protected override Size2d ComputeDesiredSize()
    {
        return SelectedWidget?.GetDesiredSize() ?? new Size2d();
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        SelectedWidget?.Collect(frame, info.AccountFor(SelectedWidget));
    }

    public override uint GetMaxSlots()
    {
        return 0;
    }

    public override void OnChildResized(Widget widget)
    {
        if (widget == SelectedWidget) base.OnChildResized(widget);
    }

    protected override void ArrangeSlots(Size2d drawSize)
    {
        if (Slots.Count == 0) return;

        var slot = Slots[SelectedIndex];

        var widget = slot.GetWidget();

        if (widget.GetDrawSize().Equals(drawSize)) return;

        widget.SetRelativeOffset(0.0f);
        widget.SetDrawSize(drawSize);
    }
}