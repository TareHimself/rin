using System.Numerics;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

/// <summary>
/// </summary>
public class FlexBox : MultiSlotCompositeView<FlexBoxSlot>
{
    private readonly FlexLayout _layout;

    public FlexBox() : this(Axis.Column)
    {
    }

    /// <summary>
    ///     A container that draws children left to right
    ///     Slot = <see cref="ListSlot" />
    /// </summary>
    public FlexBox(Axis axis)
    {
        _layout = new FlexLayout(axis, this);
    }

    public Axis Axis
    {
        get => _layout.GetAxis();
        set
        {
            _layout.SetAxis(value);
            OnDirectionChanged();
        }
    }

    public override int SlotCount => _layout.SlotCount;

    protected void OnDirectionChanged()
    {
        Invalidate(InvalidationType.Layout);
    }

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

    public override bool Add(IView child)
    {
        return _layout.Add(child);
    }

    public override bool Add(FlexBoxSlot slot)
    {
        return _layout.Add(slot);
    }

    public override bool Remove(IView child)
    {
        return _layout.Remove(child);
    }
}