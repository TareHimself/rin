using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

/// <summary>
/// Slot = <see cref="Slot"/>
/// </summary>
public class Switcher : MultiSlotCompositeView<Slot>
{
    private readonly SwitcherLayout _layout;
    
    public int SelectedIndex
    {
        get => _layout.SelectedIndex;
        set => _layout.SelectedIndex = value;
    }

    public View? SelectedView => _layout.SelectedSlot?.Child;

    public Switcher()
    {
        _layout = new SwitcherLayout(this);
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return SelectedView?.GetDesiredSize() ?? new Vector2();
    }


    protected override Vector2 ArrangeContent(Vector2 availableSpace)
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

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        _layout.Apply(GetContentSize());
    }

    public override IEnumerable<ISlot> GetSlots() => _layout.GetSlots();

    public override IEnumerable<ISlot> GetCollectableSlots() => _layout.SelectedSlot is { } slot ? [slot] : [];

    public override IEnumerable<ISlot> GetHitTestableSlots() => _layout.SelectedSlot is { } slot ? [slot] : [];

    public override int SlotCount => _layout.SlotCount;
    public override bool Add(View child) => _layout.Add(child);
    public override bool Add(Slot slot) => _layout.Add(slot);

    public override bool Remove(View child) => _layout.Remove(child);
}