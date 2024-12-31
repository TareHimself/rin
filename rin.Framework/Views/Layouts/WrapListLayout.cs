using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

public class WrapListLayout(Axis axis,CompositeView container) : ListLayout(axis, container)
{
    public override CompositeView Container { get; } = container;
    
    public override void Dispose()
    {
        
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null)
        {
            Apply(Container.GetContentSize());
        }
    }

    protected override Vector2<float> ArrangeContentRow(Vector2<float> availableSpace)
    {
        var computedSize = base.ArrangeContentRow(availableSpace);
        if (computedSize.X <= availableSpace.X)
        {
            return computedSize;
        }

        var mainAxisOffset = 0.0f;
        var crossAxisOffset = 0.0f;
        var maxCrossAxis = 0.0f;
        var totalHeight = 0.0f;
        var totalWidth = 0.0f;
        foreach (var containerSlot in GetSlots())
        {
            var offset = containerSlot.Child.Offset;
            var size = containerSlot.Child.Size;
            
            if (offset.X + size.X > availableSpace.X)
            {
                if ((mainAxisOffset + size.X) > availableSpace.X)
                {
                    if (mainAxisOffset > 0.0f)
                    {
                        crossAxisOffset += maxCrossAxis;
                        
                    }
                    
                    offset = new Vector2<float>(0.0f, crossAxisOffset);
                }
                else
                {
                    offset = new Vector2<float>(mainAxisOffset, crossAxisOffset);
                }
            }
            
            maxCrossAxis = Math.Max(size.Y, maxCrossAxis);
            
            containerSlot.Child.Offset = offset;
            mainAxisOffset = offset.X + size.X;
            totalHeight = Math.Max(totalHeight, offset.Y + size.Y);
            totalWidth = Math.Max(totalWidth, offset.X + size.X);
        }

        return new Vector2<float>( totalWidth,totalHeight);
    }
    
    protected override Vector2<float> ArrangeContentColumn(Vector2<float> availableSpace)
    {
        var computedSize = base.ArrangeContentColumn(availableSpace);
        if (computedSize.Y <= availableSpace.Y)
        {
            return computedSize;
        }

        var mainAxisOffset = 0.0f;
        var crossAxisOffset = 0.0f;
        var maxCrossAxis = 0.0f;
        var totalHeight = 0.0f;
        var totalWidth = 0.0f;
        
        foreach (var containerSlot in GetSlots())
        {
            var offset = containerSlot.Child.Offset;
            var size = containerSlot.Child.Size;
            
            if (offset.Y + size.Y > availableSpace.Y)
            {
                if ((mainAxisOffset + size.Y) > availableSpace.Y)
                {
                    if (mainAxisOffset > 0.0f)
                    {
                        crossAxisOffset += maxCrossAxis;
                        
                    }
                    
                    offset = new Vector2<float>(0.0f, crossAxisOffset);
                }
                else
                {
                    offset = new Vector2<float>(mainAxisOffset, crossAxisOffset);
                }
            }
            
            maxCrossAxis = Math.Max(size.X, maxCrossAxis);
            
            containerSlot.Child.Offset = offset;
            mainAxisOffset = offset.Y + size.Y;
            totalHeight = Math.Max(totalHeight, offset.Y + size.Y);
            totalWidth = Math.Max(totalWidth, offset.X + size.X);
        }

        return new Vector2<float>(totalWidth, totalHeight);
    }

    
    public override Vector2<float> Apply(Vector2<float> availableSpace)
    {
        return GetAxis() switch
        {
            Axis.Row => ArrangeContentRow(availableSpace),
            Axis.Column => ArrangeContentColumn(availableSpace),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public override Vector2<float> ComputeDesiredContentSize()
    {
        return GetAxis() switch
        {
            Axis.Row => GetSlots().Aggregate(new Vector2<float>(), (size, slot) =>
            {
                var slotSize = slot.Child.GetDesiredSize();
                size.X += slotSize.X;
                size.Y = System.Math.Max(size.Y, slotSize.Y);
                return size;
            }),
            Axis.Column => GetSlots().Aggregate(new Vector2<float>(), (size, slot) =>
            {
                var slotSize = slot.Child.GetDesiredSize();
                size.Y += slotSize.Y;
                size.X = System.Math.Max(size.X, slotSize.X);
                return size;
            }),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

}