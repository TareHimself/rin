using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Core;
using Rin.Engine.Core.Math;
using Rin.Engine.Views.Enums;
using Rin.Engine.Views.Layouts;

namespace Rin.Engine.Views.Composite;

/// <summary>
///     Slot = <see cref="Slot" />
/// </summary>
public class Root : MultiSlotCompositeView<Slot>
{
    private readonly RootLayout _layout;

    public Root()
    {
        _layout = new RootLayout(this);
    }

    public override int SlotCount => _layout.SlotCount;

    protected override Vector2 ComputeDesiredContentSize()
    {
        return _layout.ComputeDesiredContentSize();
    }


    protected override Vector2 ArrangeContent(Vector2 availableSpace)
    {
        return _layout.Apply(availableSpace);
    }


    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        if (_layout.FindSlot(child) is { } slot) _layout.OnSlotUpdated(slot);
    }

    public override void OnChildAdded(View child)
    {
        if (_layout.FindSlot(child) is { } slot) _layout.OnSlotUpdated(slot);
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        return _layout.GetSlots();
    }

    public override bool Add(View view)
    {
        return _layout.Add(view);
    }

    public override bool Add(Slot slot)
    {
        return _layout.Add(slot);
    }

    public override bool Remove(View view)
    {
        return _layout.Remove(view);
    }
}