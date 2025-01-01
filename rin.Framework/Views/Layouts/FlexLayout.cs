using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

public class FlexBoxSlot(FlexLayout? layout = null) : ListSlot(layout)
{
    public float? Flex = null;
}

public class FlexLayout(Axis axis, CompositeView container) : ListLayout(axis, container)
{
    
    public override ISlot MakeSlot(View widget)
    {
        return new FlexBoxSlot(this)
        {
            Child = widget
        };
    }
    
    protected override Vec2<float> ArrangeContentRow(Vec2<float> availableSpace)
    {
        var mainAxisAvailableSpace = availableSpace.X.FiniteOr();
        var space = new Vec2<float>(float.PositiveInfinity,availableSpace.Y);
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
                    var widgetSize = slot.Child.ComputeSize(new Vec2<float>(space.X,GetSlotCrossAxisSize(slot,space.Y)));
                    mainAxisAvailableSpace -= widgetSize.X;
                }
            }

            mainAxisAvailableSpace = Math.Max(mainAxisAvailableSpace, 0);
        }
        
        
        {
            var offset = new Vec2<float>(0.0f);
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

                    var flexSize = new Vec2<float>(assignedMainAxisSpace, GetSlotCrossAxisSize(slot, space.Y));

                    widget.ComputeSize(flexSize);
                    
                    slotMainAxisSize = flexSize.X;
                    slotCrossAxisSize = flexSize.Y;
                }
                else
                {
                    var widgetSize = widget.ComputeSize(new Vec2<float>(space.X,GetSlotCrossAxisSize(slot,space.Y)));
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

        return new Vec2<float>(mainAxisSize, crossAxisSize);
    }


    protected override Vec2<float> ArrangeContentColumn(Vec2<float> availableSpace)
    {
       var mainAxisAvailableSpace = availableSpace.Y.FiniteOr();
        var space = new Vec2<float>(float.PositiveInfinity,availableSpace.X);
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
                    var widgetSize = slot.Child.ComputeSize(new Vec2<float>(space.X,GetSlotCrossAxisSize(slot,space.Y)));
                    mainAxisAvailableSpace -= widgetSize.X;
                }
            }

            mainAxisAvailableSpace = Math.Max(mainAxisAvailableSpace, 0);
        }
        
        
        {
            var offset = new Vec2<float>(0.0f);
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

                    var flexSize = new Vec2<float>(GetSlotCrossAxisSize(slot,space.Y),assignedMainAxisSpace);

                    widget.ComputeSize(flexSize);
                    
                    slotMainAxisSize = flexSize.Y;
                    slotCrossAxisSize = flexSize.X;
                }
                else
                {
                    var widgetSize = widget.ComputeSize(new Vec2<float>(GetSlotCrossAxisSize(slot,space.Y),space.X));
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

        return new Vec2<float>(mainAxisSize, crossAxisSize);
    }
}