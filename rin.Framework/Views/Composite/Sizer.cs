using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Core.Animation;
using rin.Framework.Views.Animation;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

/// <summary>
/// Slot = <see cref="Slot"/>
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
    
    protected override Vec2<float> ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            var desiredSize = slot.Child.GetDesiredSize();
            return new Vec2<float>(WidthOverride ?? desiredSize.X, HeightOverride ?? desiredSize.Y);
        }

        return new Vec2<float>();
    }

    protected override Vec2<float> ArrangeContent(Vec2<float> availableSpace)
    {
        var size = new Vec2<float>(WidthOverride.GetValueOrDefault(availableSpace.X),
            HeightOverride.GetValueOrDefault(availableSpace.Y));
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = (new Vec2<float>(0, 0)); 
            return slot.Child.ComputeSize(size);
        }

        return size;
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        if (GetSlot() is { } slot)
        {
            slot.Child.Offset = 0.0f; 
            slot.Child.ComputeSize(GetContentSize());
        }
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot)
        {
            return [slot];
        }

        return [];
    }
}