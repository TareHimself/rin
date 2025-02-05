using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Core;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;





/// <summary>
/// </summary>
public class FlexBox : MultiSlotCompositeView<FlexBoxSlot>
{
    

    private readonly FlexLayout _layout;
    
    public FlexBox() : this(Axis.Column)
    {
        
    }

    /// <summary>
    /// A container that draws children left to right
    /// Slot = <see cref="ListSlot"/>
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
    
    protected void OnDirectionChanged()
    {
        Invalidate(InvalidationType.Layout);
    }

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
        Invalidate(invalidation);
    }

    public override IEnumerable<ISlot> GetSlots() =>  _layout.GetSlots();

    public override int SlotCount => _layout.SlotCount;
    public override bool Add(View child) => _layout.Add(child);
    public override bool Add(FlexBoxSlot slot) => _layout.Add(slot);
    public override bool Remove(View child) => _layout.Remove(child);
}