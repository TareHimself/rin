using rin.Core.Math;
using rin.Widgets.Enums;

namespace rin.Widgets.Containers;

/// <summary>
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class Sizer : Container
{
    private float? _heightOverride;
    private float? _widthOverride;

    public float? WidthOverride
    {
        get => _widthOverride;
        set
        {
            _widthOverride = value;
            Invalidate(InvalidationType.Layout);
        }
    }

    public float? HeightOverride
    {
        get => _heightOverride;
        set
        {
            _heightOverride = value;
            Invalidate(InvalidationType.Layout);
        }
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (GetSlot(0) is { } slot)
        {
            var desiredSize = slot.Child.GetDesiredSize();
            return new Vector2<float>(WidthOverride ?? desiredSize.X, HeightOverride ?? desiredSize.Y);
        }

        return new Vector2<float>();
    }

    public override int GetMaxSlotsCount() => 1;

    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        var size = new Vector2<float>(WidthOverride.GetValueOrDefault(availableSpace.X),
            HeightOverride.GetValueOrDefault(availableSpace.Y));
        if (GetSlot(0) is { } slot)
        {
            slot.Child.Offset = (new Vector2<float>(0, 0)); 
            return slot.Child.ComputeSize(size);
        }

        return size;
    }
}