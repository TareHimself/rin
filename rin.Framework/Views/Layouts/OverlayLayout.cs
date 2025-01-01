using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;


public class OverlayLayout(CompositeView container) : InfiniteChildrenLayout
{
    public override CompositeView Container { get; } = container;
    public override int MaxSlotCount => int.MaxValue;
    
    public override void Dispose()
    {
        
    }

    public override ISlot MakeSlot(View widget)
    {
        return new Slot(this)
        {
            Child = widget
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null)
        {
            Apply(Container.GetContentSize());
        }
    }
    
    public override Vec2<float> Apply(Vec2<float> availableSpace)
    {
        var dims = new Vec2<float>(0.0f);
        
        // First pass is for widgets with content to figure out their size
        foreach (var slot in GetSlots())
        {
            var desiredSize = slot.Child.GetDesiredSize();
            if(desiredSize.X <= 0.0f || desiredSize.Y <= 0.0f) continue;
            var widgetSize = slot.Child.ComputeSize(availableSpace);
            
            dims.X = Math.Max(dims.X, widgetSize.X.FiniteOr());
            dims.Y = Math.Max(dims.Y, widgetSize.Y.FiniteOr());
        }
        
        // Second pass is for widgets that adapt to the size of the container
        foreach (var slot in GetSlots())
        {
            slot.Child.Offset = (new Vec2<float>(0, 0));
            slot.Child.ComputeSize(dims);
        }
        return new Vec2<float>(Math.Min(dims.X,availableSpace.X),Math.Min(dims.Y,availableSpace.Y));
    }

    public override Vec2<float> ComputeDesiredContentSize()
    {
        return GetSlots().Aggregate(new Vec2<float>(), (size, slot) =>
        {
            var slotSize = slot.Child.GetDesiredSize();
            size.Y = System.Math.Max(size.Y, slotSize.Y);
            size.X = System.Math.Max(size.X, slotSize.X);
            return size;
        });
    }
}