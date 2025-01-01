using rin.Framework.Core.Math;
using rin.Framework.Core.Extensions;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Events;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

/// <summary>
/// A container that draws children on top of each other
/// </summary>
public class Overlay : MultiSlotCompositeView<Slot>
{
    private readonly OverlayLayout _layout;

    public Overlay() : base()
    {
        _layout = new OverlayLayout(this);
    }
    
    protected override Vec2<float> ComputeDesiredContentSize()
    {
        return _layout.ComputeDesiredContentSize();
    }


    protected override Vec2<float> ArrangeContent(Vec2<float> availableSpace)
    {
        return _layout.Apply(availableSpace);
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        Invalidate(invalidation);
    }

    public override IEnumerable<ISlot> GetSlots() => _layout.GetSlots();
    
    public override IEnumerable<ISlot> GetHitTestableSlots() => _layout.GetSlots().AsReversed();

    public override int SlotCount => _layout.SlotCount;
    public override bool Add(View child) => _layout.Add(child);
    public override bool Add(Slot slot) => _layout.Add(slot);
    public override bool Remove(View child) => _layout.Remove(child);
}