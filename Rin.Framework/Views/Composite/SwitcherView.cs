using System.Numerics;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

/// <summary>
///     Slot = <see cref="Slot" />
/// </summary>
public class SwitcherView : MultiSlotCompositeView<Slot>
{
    private readonly SwitcherLayout _layout;

    public SwitcherView()
    {
        _layout = new SwitcherLayout(this);
    }

    public int SelectedIndex
    {
        get => _layout.SelectedIndex;
        set => _layout.SelectedIndex = value;
    }

    public IView? SelectedView => _layout.SelectedSlot?.Child;

    public override int SlotCount => _layout.SlotCount;

    public override Vector2 ComputeDesiredContentSize()
    {
        return SelectedView?.GetDesiredSize() ?? Vector2.Zero;
    }


    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        if (_layout.SelectedSlot is { } slot)
        {
            var view = slot.Child;

            if (view.Size.Equals(availableSpace)) return view.Size;

            view.Offset = default;
            return view.ComputeSize(availableSpace);
        }

        return availableSpace;
    }

    public override void OnChildInvalidated(IView child, InvalidationType invalidation)
    {
        _layout.Apply(GetContentSize());
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        return _layout.GetSlots();
    }

    public override IEnumerable<ISlot> GetCollectableSlots()
    {
        return _layout.SelectedSlot is { } slot ? [slot] : [];
    }

    public override IEnumerable<ISlot> GetHitTestableSlots()
    {
        return _layout.SelectedSlot is { } slot ? [slot] : [];
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