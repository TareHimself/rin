using System.Numerics;
using Rin.Framework.Extensions;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

/// <summary>
///     A container that draws children on top of each other
/// </summary>
public class Overlay : MultiSlotCompositeView<Slot>
{
    private readonly OverlayLayout _layout;

    public Overlay()
    {
        _layout = new OverlayLayout(this);
    }

    public override int SlotCount => _layout.SlotCount;

    public override Vector2 ComputeDesiredContentSize()
    {
        return _layout.ComputeDesiredContentSize();
    }


    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        return _layout.Apply(availableSpace);
    }

    public override void OnChildInvalidated(IView child, InvalidationType invalidation)
    {
        Invalidate(invalidation);
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        return _layout.GetSlots();
    }

    public override IEnumerable<ISlot> GetHitTestableSlots()
    {
        return _layout.GetSlots().AsReversed();
    }

    public override bool Add(IView child)
    {
        return _layout.Add(child);
    }

    public override bool Add(Slot slot)
    {
        return _layout.Add(slot);
    }

    public override bool Remove(IView child)
    {
        return _layout.Remove(child);
    }
}