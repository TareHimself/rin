using System.Numerics;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

/// <summary>
///     A container that draws children based on the settings provided in <see cref="PanelSlot" /> . Intended use is for
///     dock-able layouts or as a root for a collection of views
/// </summary>
public class PanelView : MultiSlotCompositeView<PanelSlot>
{
    private readonly PanelLayout _layout;


    public PanelView()
    {
        _layout = new PanelLayout(this);
    }

    public override int SlotCount => _layout.SlotCount;

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        return _layout.Apply(availableSpace);
    }

    public override void OnChildLayoutInvalidated(IView child)
    {
        // We do nothing because our size is decided by parents
    }

    public override void OnChildRemoved(IView child)
    {
        // We do nothing because our size is decided by parents
    }

    public override void LayoutChild(IView child)
    {
        if (_layout.FindSlot(child) is { } slot) _layout.OnSlotUpdated(slot);
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        return _layout.GetSlots();
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return _layout.ComputeDesiredContentSize();
    }

    public override bool Add(IView child)
    {
        return _layout.Add(child);
    }

    public override bool Add(PanelSlot slot)
    {
        return _layout.Add(slot);
    }

    public override bool Remove(IView child)
    {
        return _layout.Remove(child);
    }
}