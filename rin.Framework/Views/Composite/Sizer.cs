using System.Numerics;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

/// <summary>
///     Slot = <see cref="Slot" />
/// </summary>
public class Sizer : SingleSlotCompositeView
{
    private float? _heightOverride;
    private float? _widthOverride;

    public float? WidthOverride
    {
        get => _widthOverride;
        set
        {
            _widthOverride = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    public float? HeightOverride
    {
        get => _heightOverride;
        set
        {
            _heightOverride = value;
            Invalidate(InvalidationType.DesiredSize);
        }
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            var desiredSize = slot.Child.GetDesiredSize();
            return new Vector2(WidthOverride ?? desiredSize.X, HeightOverride ?? desiredSize.Y);
        }

        return new Vector2();
    }

    protected override Vector2 ArrangeContent(Vector2 availableSpace)
    {
        var size = new Vector2(WidthOverride.GetValueOrDefault(availableSpace.X),
            HeightOverride.GetValueOrDefault(availableSpace.Y));
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = default;
            size = slot.Child.ComputeSize(size);
        }

        return new Vector2(WidthOverride.GetValueOrDefault(size.X), HeightOverride.GetValueOrDefault(size.Y));
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = default;
            slot.Child.ComputeSize(GetContentSize());
        }
    }


    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot) return [slot];

        return [];
    }
}