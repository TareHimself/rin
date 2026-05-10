using System.Numerics;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

/// <summary>
///     Slot = <see cref="Slot" />
/// </summary>
public class SizerView : SingleSlotCompositeView
{
    private float? _heightOverride;
    private float? _widthOverride;

    public float? WidthOverride
    {
        get => _widthOverride;
        set
        {
            _widthOverride = value;
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    }

    public float? HeightOverride
    {
        get => _heightOverride;
        set
        {
            _heightOverride = value;
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            var desiredSize = slot.Child.GetDesiredSize();
            return new Vector2(WidthOverride ?? desiredSize.X, HeightOverride ?? desiredSize.Y);
        }

        return new Vector2();
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        var size = new Vector2(WidthOverride.GetValueOrDefault(availableSpace.X),
            HeightOverride.GetValueOrDefault(availableSpace.Y));
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = default;
            size = slot.Child.Layout(size);
        }

        return new Vector2(WidthOverride.GetValueOrDefault(size.X), HeightOverride.GetValueOrDefault(size.Y));
    }

    public override void LayoutChild(IView child)
    {
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = default;
            slot.Child.Layout(GetContentSize());
        }
    }


    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot) return [slot];

        return [];
    }
}