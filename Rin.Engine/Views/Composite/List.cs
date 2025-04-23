using System.Numerics;
using Rin.Engine.Views.Enums;
using Rin.Engine.Views.Layouts;

namespace Rin.Engine.Views.Composite;

/// <summary>
///     A container that draws children left to right
/// </summary>
public class List : MultiSlotCompositeView<ListSlot>
{
    private readonly ListLayout _layout;

    public List() : this(Axis.Column)
    {
    }

    /// <summary>
    ///     A container that draws children left to right
    ///     Slot = <see cref="ListSlot" />
    /// </summary>
    public List(Axis axis)
    {
        _layout = new ListLayout(axis, this);
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

    protected virtual void OnDirectionChanged()
    {
        Invalidate(InvalidationType.Layout);
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return _layout.ComputeDesiredContentSize();
    }

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        return _layout.Apply(availableSpace);
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        Invalidate(invalidation);
    }

    public override IEnumerable<ISlot> GetSlots()
    {
        return _layout.GetSlots();
    }

    public override bool Add(View child)
    {
        return _layout.Add(child);
    }

    public override bool Add(ListSlot slot)
    {
        return _layout.Add(slot);
    }

    public override bool Remove(View child)
    {
        return _layout.Remove(child);
    }
}