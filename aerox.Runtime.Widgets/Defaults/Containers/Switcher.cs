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
            _selected = System.Math.Clamp(value, 0, slots.Count == 0 ? 0 : slots.Count - 1);
            if (lastSelected != _selected) SelectedWidgetUpdated();
        }
    }

    public Widget? SelectedWidget => slots.Count > SelectedIndex ? slots[SelectedIndex].GetWidget() : null;

    public void SelectedWidgetUpdated()
    {
        CheckSize();
        ArrangeSlots(GetDrawSize());
    }

    public override Size2d ComputeDesiredSize()
    {
        return SelectedWidget?.GetDesiredSize() ?? new Size2d();
    }

    public override void Draw(WidgetFrame frame, DrawInfo info)
    {
        SelectedWidget?.Draw(frame, info.AccountFor(SelectedWidget));
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
        if (slots.Count == 0) return;

        var slot = slots[SelectedIndex];

        var widget = slot.GetWidget();

        if (widget.GetDrawSize().Equals(drawSize)) return;

        widget.SetRelativeOffset(0.0f);
        widget.SetDrawSize(drawSize);
    }
}