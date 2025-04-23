using System.Numerics;
using Rin.Engine.Views.Enums;
using Rin.Engine.Views.Layouts;

namespace Rin.Engine.Views.Composite;

/// <summary>
///     A container that draws children based on the settings provided in <see cref="PanelSlot" /> . Intended use is for
///     dock-able layouts or as a root for a collection of views
/// </summary>
public class Panel : MultiSlotCompositeView<PanelSlot>
{
    private readonly PanelLayout _layout;


    public Panel()
    {
        _layout = new PanelLayout(this);
    }

    public override int SlotCount => _layout.SlotCount;

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
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

    protected override Vector2 ComputeDesiredContentSize()
    {
        return _layout.ComputeDesiredContentSize();
    }

    public override bool Add(View child)
    {
        return _layout.Add(child);
    }

    public override bool Add(PanelSlot slot)
    {
        return _layout.Add(slot);
    }

    public override bool Remove(View child)
    {
        return _layout.Remove(child);
    }
}