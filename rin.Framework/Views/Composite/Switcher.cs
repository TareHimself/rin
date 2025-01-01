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

    public View? SelectedWidget => _layout.SelectedSlot?.Child;

    public Switcher()
    {
        _layout = new SwitcherLayout(this);
    }

    protected override Vec2<float> ComputeDesiredContentSize()
    {
        return SelectedWidget?.GetDesiredSize() ?? new Vec2<float>();
    }


    protected override Vec2<float> ArrangeContent(Vec2<float> availableSpace)
    {
        if (_layout.SelectedSlot is { } slot)
        {
            var widget = slot.Child;

            if (widget.Size.Equals(availableSpace)) return widget.Size;

            widget.Offset = (0.0f);
            return widget.ComputeSize(availableSpace);
        }

        return 0.0f;
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        _layout.Apply(GetContentSize());
    }

    public override IEnumerable<ISlot> GetSlots() => _layout.SelectedSlot is { } slot ? [slot] : [];

    public override int SlotCount => _layout.SlotCount;
    public override bool Add(View child) => _layout.Add(child);
    public override bool Add(Slot slot) => _layout.Add(slot);

    public override bool Remove(View child) => _layout.Remove(child);
}