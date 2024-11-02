using rin.Core;
using rin.Core.Math;

namespace rin.Widgets.Containers;


public class FlexContainerSlot(FlexContainer? container = null) : ListContainerSlot(container)
{
    public float? Flex = null;
}


/// <summary>
/// Slot = <see cref="FlexContainerSlot"/>
/// </summary>
public class FlexContainer : ListContainer
{
    protected override void ArrangeSlots(Vector2<float> drawSize)
    {
        Dictionary<Widget,float> flexWidgets = [];
        var flexTotal = 0.0f;
        Vector2<float> availableSpace = drawSize;
        
        var slots = GetSlots();
        
        foreach (var slot in slots)
        {
            var widget = slot.Child;
            if (slot is FlexContainerSlot { Flex: not null } asFlex)
            {
                var flex = asFlex.Flex.Value;
                flexWidgets.Add(widget,flex);
                flexTotal += flex;
            }
            else
            {
                var desiredSize = widget.GetDesiredSize();
                switch (Axis)
                {
                    case Axis.Column:
                        availableSpace.Y -= desiredSize.Y;
                        break;
                    case Axis.Row:
                        availableSpace.X -= desiredSize.X;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        availableSpace = new Vector2<float>(Math.Max(0, availableSpace.X), Math.Max(0, availableSpace.Y));

        var offset = new Vector2<float>();


        switch (Axis)
        {
            case Axis.Column:
            {
                foreach (var slot in slots)
                {
                    if(slot is not FlexContainerSlot asFlexContainerSlot) continue;
                    var widget = slot.Child;
                    Vector2<float> size = widget.GetDesiredSize();
                        
                    if (flexWidgets.TryGetValue(widget, out var flex))
                    {
                        size.Y = availableSpace.Y * (flex / flexTotal);
                    }
                    
                    widget.Offset = (offset.Clone());
                    widget.Size = (size);
                    
                    HandleCrossAxis(asFlexContainerSlot,drawSize);
                    
                    offset.Y += size.Y;
                }
            }
                break;
            case Axis.Row:
            {
                foreach (var slot in slots)
                {
                    if(slot is not FlexContainerSlot asFlexContainerSlot) continue;
                    var widget = slot.Child;
                    Vector2<float> size = widget.GetDesiredSize();
                        
                    if (flexWidgets.TryGetValue(widget, out var flex))
                    {
                        size.X = availableSpace.X * (flex / flexTotal);
                    }
                        
                    widget.Offset = (offset.Clone());
                    widget.Size = (size);
                    
                    HandleCrossAxis(asFlexContainerSlot,drawSize);
                    
                    offset.X += size.X;
                }
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    public override ContainerSlot MakeSlot(Widget widget) => new FlexContainerSlot(this)
    {
        Child = widget,
    };
}