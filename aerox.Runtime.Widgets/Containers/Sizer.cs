using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Containers;

public class Sizer : Container
{
    private float? _heightOverride;
    private float? _widthOverride;

    public Sizer(Widget child) : base([child])
    {
    }

    public Sizer()
    {
    }

    public float? WidthOverride
    {
        get => _widthOverride;
        set
        {
            _widthOverride = value;
            CheckSize();
        }
    }

    public float? HeightOverride
    {
        get => _heightOverride;
        set
        {
            _heightOverride = value;
            CheckSize();
        }
    }

    protected override Size2d ComputeContentDesiredSize()
    {
        if (GetSlot(0) is { } slot)
        {
            var desiredSize = slot.GetWidget().GetDesiredSize();
            return new Size2d(WidthOverride ?? desiredSize.Width, HeightOverride ?? desiredSize.Height);
        }

        return new Size2d();
    }

    public override uint GetMaxSlots() => 1;

    protected override void ArrangeSlots(Size2d drawSize)
    {
        if (GetSlot(0) is { } slot)
        {
            slot.GetWidget().SetOffset(new Vector2<float>(0, 0));
            slot.GetWidget().SetSize(drawSize.Clone());
        }
    }
}