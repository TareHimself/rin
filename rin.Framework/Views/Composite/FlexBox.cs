using rin.Framework.Core.Math;
using rin.Framework.Core;

namespace rin.Framework.Views.Composite;


public class FlexBoxSlot(FlexBox? container = null) : ListSlot(container)
{
    public float? Flex = null;
}


/// <summary>
/// Slot = <see cref="FlexBoxSlot"/>
/// </summary>
public class FlexBox : List
{
    protected override Vector2<float> ArrangeContentRow(Vector2<float> availableSpace)
    {
        var mainAxisAvailableSpace = availableSpace.X.FiniteOr();
        var space = new Vector2<float>(float.PositiveInfinity,availableSpace.Y);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
        var flexTotal = 0.0f;
        var slots = GetSlots();
        
        if (mainAxisAvailableSpace > 0.0f)
        {
            
            foreach (var slot in slots)
            {
                if (slot is FlexBoxSlot {Flex: not null} asFlexSlot)
                {
                    flexTotal += asFlexSlot.Flex.Value;
                }
                else
                {
                    var widgetSize = slot.Child.ComputeSize(new Vector2<float>(space.X,GetSlotCrossAxisSize(slot,space.Y)));
                    mainAxisAvailableSpace -= widgetSize.X;
                }
            }

            mainAxisAvailableSpace = Math.Max(mainAxisAvailableSpace, 0);
        }
        
        
        {
            var offset = new Vector2<float>(0.0f);
            // Compute slot sizes and initial offsets
            foreach (var slot in slots)
            {
                var widget = slot.Child;
                widget.Offset = offset;

                var slotMainAxisSize = 0.0f;
                var slotCrossAxisSize = 0.0f;
                if (slot is FlexBoxSlot { Flex: not null } asFlexSlot)
                {
                    var assignedMainAxisSpace =
                        flexTotal > 0.0f ? mainAxisAvailableSpace * (asFlexSlot.Flex.Value / flexTotal) : 0.0f;

                    var flexSize = new Vector2<float>(assignedMainAxisSpace, GetSlotCrossAxisSize(slot, space.Y));

                    widget.ComputeSize(flexSize);
                    
                    slotMainAxisSize = flexSize.X;
                    slotCrossAxisSize = flexSize.Y;
                }
                else
                {
                    var widgetSize = widget.ComputeSize(new Vector2<float>(space.X,GetSlotCrossAxisSize(slot,space.Y)));
                    slotMainAxisSize = widgetSize.X;
                    slotCrossAxisSize = widgetSize.Y;
                }
                
                offset.X += slotMainAxisSize;
                mainAxisSize += slotMainAxisSize;
                crossAxisSize = Math.Max(crossAxisSize,slotCrossAxisSize);
            }

            crossAxisSize = float.IsFinite(space.Y) ? space.Y : crossAxisSize;
        }
        
        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot,crossAxisSize);
        }

        return new Vector2<float>(mainAxisSize, crossAxisSize);
    }


    protected override Vector2<float> ArrangeContentColumn(Vector2<float> availableSpace)
    {
       var mainAxisAvailableSpace = availableSpace.Y.FiniteOr();
        var space = new Vector2<float>(float.PositiveInfinity,availableSpace.X);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
        var flexTotal = 0.0f;
        var slots = GetSlots();
        
        if (mainAxisAvailableSpace > 0.0f)
        {
            
            foreach (var slot in slots)
            {
                if (slot is FlexBoxSlot {Flex: not null} asFlexSlot)
                {
                    flexTotal += asFlexSlot.Flex.Value;
                }
                else
                {
                    var widgetSize = slot.Child.ComputeSize(new Vector2<float>(space.X,GetSlotCrossAxisSize(slot,space.Y)));
                    mainAxisAvailableSpace -= widgetSize.X;
                }
            }

            mainAxisAvailableSpace = Math.Max(mainAxisAvailableSpace, 0);
        }
        
        
        {
            var offset = new Vector2<float>(0.0f);
            // Compute slot sizes and initial offsets
            foreach (var slot in slots)
            {
                var widget = slot.Child;
                widget.Offset = offset;

                var slotMainAxisSize = 0.0f;
                var slotCrossAxisSize = 0.0f;
                if (slot is FlexBoxSlot { Flex: not null } asFlexSlot)
                {
                    var assignedMainAxisSpace =
                        flexTotal > 0.0f ? mainAxisAvailableSpace * (asFlexSlot.Flex.Value / flexTotal) : 0.0f;

                    var flexSize = new Vector2<float>(GetSlotCrossAxisSize(slot,space.Y),assignedMainAxisSpace);

                    widget.ComputeSize(flexSize);
                    
                    slotMainAxisSize = flexSize.Y;
                    slotCrossAxisSize = flexSize.X;
                }
                else
                {
                    var widgetSize = widget.ComputeSize(new Vector2<float>(GetSlotCrossAxisSize(slot,space.Y),space.X));
                    slotMainAxisSize = widgetSize.Y;
                    slotCrossAxisSize = widgetSize.X;
                }
                    
                offset.Y += slotMainAxisSize;
                mainAxisSize += slotMainAxisSize;
                crossAxisSize = Math.Max(crossAxisSize,slotCrossAxisSize);
            }

            crossAxisSize = float.IsFinite(space.X) ? space.X : crossAxisSize;
        }
        
        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot,crossAxisSize);
        }

        return new Vector2<float>(mainAxisSize, crossAxisSize);
    }

    protected override CompositeViewSlot MakeSlot(View view) => new FlexBoxSlot(this)
    {
        Child = view,
    };
}