using rin.Core.Math;

namespace rin.Widgets.Containers;

/// <summary>
/// Needs work, do not use
/// Slot = <see cref="ContainerSlot"/>
/// </summary>
public class WrapList : List
{
    protected override Vector2<float> ArrangeContentColumn(Vector2<float> availableSpace)
    {
        var computedSize = base.ArrangeContentRow(availableSpace);
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
    // protected override void ArrangeContent(Vector2<float> drawSize)
    // {
    //     var offset = new Vector2<float>(0.0f);
    //     var rowHeight = 0.0f;
    //
    //     foreach (var slot in GetSlots())
    //     {
    //         var widget = slot.Child;
    //         widget.Size = (widget.GetDesiredSize());
    //         var widgetDrawSize = widget.GetContentSize();
    //         if (offset.X + widgetDrawSize.X > drawSize.X)
    //             if (offset.X != 0)
    //             {
    //                 offset.X = 0.0f;
    //                 offset.Y += rowHeight;
    //             }
    //
    //         widget.Offset = (offset.Clone());
    //
    //         rowHeight = System.Math.Max(rowHeight, widgetDrawSize.Y);
    //
    //         offset.X += widgetDrawSize.X;
    //
    //         if (!(offset.X >= drawSize.X)) continue;
    //
    //         offset.X = 0.0f;
    //         offset.Y += rowHeight;
    //     }
    // }
}