using rin.Core;
using rin.Core.Math;

namespace rin.Widgets.Containers;


public class FlexBoxSlot(FlexBox? container = null) : ListContainerSlot(container)
{
    public float? Flex = null;
}


/// <summary>
/// Slot = <see cref="FlexBoxSlot"/>
/// </summary>
public class FlexBox : List
{
    // protected override void ArrangeContent(Vector2<float> drawSize)
    // {
    //     Dictionary<Widget,float> flexWidgets = [];
    //     var flexTotal = 0.0f;
    //     Vector2<float> availableSpace = drawSize;
    //     
    //     var slots = GetSlots();
    //     
    //     foreach (var slot in slots)
    //     {
    //         var widget = slot.Child;
    //         if (slot is FlexBoxSlot { Flex: not null } asFlex)
    //         {
    //             var flex = asFlex.Flex.Value;
    //             flexWidgets.Add(widget,flex);
    //             flexTotal += flex;
    //         }
    //         else
    //         {
    //             var desiredSize = widget.GetDesiredSize();
    //             switch (Axis)
    //             {
    //                 case Axis.Column:
    //                     availableSpace.Y -= desiredSize.Y;
    //                     break;
    //                 case Axis.Row:
    //                     availableSpace.X -= desiredSize.X;
    //                     break;
    //                 default:
    //                     throw new ArgumentOutOfRangeException();
    //             }
    //         }
    //     }
    //
    //     availableSpace = new Vector2<float>(Math.Max(0, availableSpace.X), Math.Max(0, availableSpace.Y));
    //
    //     var offset = new Vector2<float>();
    //
    //
    //     switch (Axis)
    //     {
    //         case Axis.Column:
    //         {
    //             foreach (var slot in slots)
    //             {
    //                 if(slot is not FlexBoxSlot asFlexContainerSlot) continue;
    //                 var widget = slot.Child;
    //                 Vector2<float> size = widget.GetDesiredSize();
    //                     
    //                 if (flexWidgets.TryGetValue(widget, out var flex))
    //                 {
    //                     size.Y = availableSpace.Y * (flex / flexTotal);
    //                 }
    //                 
    //                 widget.Offset = (offset.Clone());
    //                 widget.Size = (size);
    //                 
    //                 HandleCrossAxisOffset(asFlexContainerSlot,drawSize);
    //                 
    //                 offset.Y += size.Y;
    //             }
    //         }
    //             break;
    //         case Axis.Row:
    //         {
    //             foreach (var slot in slots)
    //             {
    //                 if(slot is not FlexBoxSlot asFlexContainerSlot) continue;
    //                 var widget = slot.Child;
    //                 Vector2<float> size = widget.GetDesiredSize();
    //                     
    //                 if (flexWidgets.TryGetValue(widget, out var flex))
    //                 {
    //                     size.X = availableSpace.X * (flex / flexTotal);
    //                 }
    //                     
    //                 widget.Offset = (offset.Clone());
    //                 widget.Size = (size);
    //                 
    //                 HandleCrossAxisOffset(asFlexContainerSlot,drawSize);
    //                 
    //                 offset.X += size.X;
    //             }
    //         }
    //             break;
    //         default:
    //             throw new ArgumentOutOfRangeException();
    //     }
    //     
    // }

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
            if (slot is not ListContainerSlot asListContainerSlot) continue;
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
            if (slot is not ListContainerSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot,crossAxisSize);
        }

        return new Vector2<float>(mainAxisSize, crossAxisSize);
    }

    protected override ContainerSlot MakeSlot(Widget widget) => new FlexBoxSlot(this)
    {
        Child = widget,
    };
}