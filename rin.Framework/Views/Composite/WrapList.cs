using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

/// <summary>
/// Needs work, do not use
/// </summary>
public class WrapList : MultiSlotCompositeView<ListSlot>
{
    private readonly WrapListLayout _layout;

   
    public WrapList() : this(Axis.Column)
    {
        
    }

    /// <summary>
    /// A container that draws children left to right
    /// Slot = <see cref="ListSlot"/>
    /// </summary>
    public WrapList(Axis axis)
    {
        _layout = new WrapListLayout(axis, this);
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
    
    protected void OnDirectionChanged()
    {
        Invalidate(InvalidationType.Layout);
    }

    protected override Vec2<float> ComputeDesiredContentSize()
    {
        return _layout.ComputeDesiredContentSize();
    }
    
    protected override Vec2<float> ArrangeContent(Vec2<float> availableSpace)
    {
        return _layout.Apply(availableSpace);
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        Invalidate(invalidation);
    }

    public override IEnumerable<ISlot> GetSlots() =>  _layout.GetSlots();

    public override int SlotCount => _layout.SlotCount;
    public override bool Add(View child) => _layout.Add(child);
    public override bool Add(ListSlot slot) => _layout.Add(slot);
    public override bool Remove(View child) => _layout.Remove(child);
}