using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

public class SimpleSlot : ISlot
{
    public required View Child { get; set; }
    public void SetLayout(ILayout layout)
    {
        
    }
}
public abstract class SingleSlotCompositeView : CompositeView
{
    private SimpleSlot? _slot;
    
    /// <summary>
    /// Adds the View to this container
    /// </summary>
    public View? Child
    {
        init => SetChild(value);
    }
    
    [PublicAPI]
    public void SetChild(View? child)
    {
        _slot?.Child.SetParent(null);

        if (child != null)
        {
            if (_slot == null)
            {
                _slot = new SimpleSlot
                {
                    Child = child,
                };
            }
            else
            {
                _slot.Child = child;
            }
                
            _slot.Child.SetParent(this);
                
            Invalidate(InvalidationType.Layout);
        }
        else
        {
            _slot = null;
        }
    }
    
    [PublicAPI]
    public View? GetChild() => _slot?.Child;
    
    [PublicAPI]
    protected ISlot? GetSlot() => _slot;
    
    public override IEnumerable<ISlot> GetSlots()
    {
        if (GetSlot() is { } slot)
        {
            return [slot];
        }

        return [];
    }
    
    protected override Vec2<float> ComputeDesiredContentSize()
    {
        if (GetSlot() is { } slot)
        {
            return slot.Child.GetDesiredSize();
        }

        return new Vec2<float>();
    }

    public override void OnChildInvalidated(View child, InvalidationType invalidation)
    {
        Invalidate(invalidation);
    }

    // public override void OnChildInvalidated(View child, InvalidationType invalidation)
    // {
    //     Invalidate(invalidation);
    // }
}