using rin.Core.Math;

namespace rin.Widgets.Containers;

public class WCSizer : Container
{
    private float? _heightOverride;
    private float? _widthOverride;

    public WCSizer(Widget child) : base([child])
    {
    }

    public WCSizer()
    {
    }

    public float? WidthOverride
    {
        get => _widthOverride;
        set
        {
            _widthOverride = value;
            TryUpdateDesiredSize();
        }
    }

    public float? HeightOverride
    {
        get => _heightOverride;
        set
        {
            _heightOverride = value;
            TryUpdateDesiredSize();
        }
    }

    protected override Size2d ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            var desiredSize = slot.GetWidget().GetDesiredSize();
            return new Size2d(WidthOverride ?? desiredSize.Width, HeightOverride ?? desiredSize.Height);
        }

        return new Size2d();
    }

    public override int GetMaxSlots() => 1;

    protected override void ArrangeSlots(Size2d drawSize)
    {
        if (GetSlot(0) is { } slot)
        {
            slot.GetWidget().SetOffset(new Vector2<float>(0, 0));
            slot.GetWidget().SetSize(drawSize.Clone());
        }
    }
}