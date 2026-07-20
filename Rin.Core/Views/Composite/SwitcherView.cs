using System.Numerics;
using Rin.Core.Views.Layouts;

namespace Rin.Core.Views.Composite;

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

            // Must check IsLayoutValid: an invalid child at the right size would skip Layout(),
            // leaving IsLayoutValid false and causing MaybeForceLayout to re-trigger every read.
            if (view.IsLayoutValid && view.GetSize().Equals(availableSpace)) return view.GetSize();

            view.Offset = default;
            return view.Layout(availableSpace);
        }

        return availableSpace;
    }

    public override ISlot[] GetSlots()
    {
        return _layout.SelectedSlot is null ? [] : [_layout.SelectedSlot];
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

    public override IView[] GetChildren()
    {
        var slots = GetSlots();
        var children = new IView[slots.Length];
        for (var i = 0; i < slots.Length; i++)
        {
            children[i] = slots[i].Child;
        }

        return children;
    }
}