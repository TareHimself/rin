using rin.Framework.Core.Math;
using rin.Framework.Widgets.Enums;

namespace rin.Framework.Widgets.Containers;


public enum Axis
{
    Column,
    Row
}

public enum CrossFit
{
    Desired,
    Fill
}

public enum CrossAlign
{
    Start,
    Center,
    End
}

public class ListSlot(List? container = null) : ContainerSlot(container)
{
    public CrossFit Fit = CrossFit.Desired;
    public CrossAlign Align = CrossAlign.Start;
}

/// <summary>
/// A container that draws children left to right
/// Slot = <see cref="ListSlot"/>
/// </summary>
public class List(Axis axis) : ContainerWidget
{
    public List() : this(Axis.Column)
    {
        
    }

    public Axis Axis
    {
        get => axis;
        set
        {
            axis = value;
            OnDirectionChanged();
        }
    }

    protected virtual void OnDirectionChanged()
    {
        Invalidate(InvalidationType.Layout);
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return axis switch
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

    protected virtual Vector2<float> ArrangeContentRow(Vector2<float> availableSpace)
    {
        var offset = new Vector2<float>(0.0f);
        
        var space = new Vector2<float>(float.PositiveInfinity,availableSpace.Y);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
        var slots = GetSlots().ToArray();
        
        foreach (var slot in slots)
        {
            var widget = slot.Child;
            widget.Offset = offset;
                    
            var widgetSize = widget.ComputeSize(new Vector2<float>(space.X,GetSlotCrossAxisSize(slot,space.Y)));
                    
            offset.X += widgetSize.X;
            mainAxisSize += widgetSize.X;
            crossAxisSize = Math.Max(crossAxisSize, widgetSize.Y);
        }

        crossAxisSize = float.IsFinite(space.Y) ? space.Y : crossAxisSize;
                
        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot,crossAxisSize);
        }

        return new Vector2<float>(mainAxisSize, crossAxisSize);
    }
    
    protected virtual Vector2<float> ArrangeContentColumn(Vector2<float> availableSpace)
    {
        var offset = new Vector2<float>(0.0f);
        
        var space = new Vector2<float>(availableSpace.X,float.PositiveInfinity);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
                    
        var slots = GetSlots().ToArray();
                
        // Compute slot sizes and initial offsets
        foreach (var slot in slots)
        {
            var widget = slot.Child;
            widget.Offset = offset;
                    
            var widgetSize = widget.ComputeSize(new Vector2<float>(GetSlotCrossAxisSize(slot,space.X),space.Y));
                    
            offset.Y += widgetSize.Y;
            mainAxisSize += widgetSize.Y;
            crossAxisSize = Math.Max(crossAxisSize, widgetSize.X);
        }

        crossAxisSize = float.IsFinite(space.X) ? space.X : crossAxisSize;
                
        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot,crossAxisSize);
        }

        return new Vector2<float>(crossAxisSize,mainAxisSize);
    }


    protected override Vector2<float> ArrangeContent(Vector2<float> availableSpace)
    {
        return axis switch
        {
            Axis.Row => ArrangeContentRow(availableSpace),
            Axis.Column => ArrangeContentColumn(availableSpace),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected virtual float GetSlotCrossAxisSize(ContainerSlot slot, float crossAxisAvailableSize)
    {
        if (slot is ListSlot asListContainerSlot)
        {
            return asListContainerSlot.Fit switch
            {
                CrossFit.Desired => Math.Clamp(asListContainerSlot.Child.GetDesiredSize().X,0.0f,crossAxisAvailableSize),
                CrossFit.Fill => crossAxisAvailableSize,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return 0.0f;
    }

    protected virtual void HandleCrossAxisOffset(ListSlot slot,float crossAxisSize)
    {
        var widget = slot.Child;
        var size = widget.Size;
        switch (Axis)
        {
            case Axis.Column:
            {
                if (slot.Fit != CrossFit.Fill)
                {
                    var offset = widget.Offset;
                    offset.X = slot.Align switch
                    {
                        CrossAlign.Start => 0.0f,
                        CrossAlign.Center => (crossAxisSize / 2.0f) - (size.X / 2.0f),
                        CrossAlign.End => size.X - crossAxisSize,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    widget.Offset = (offset);
                }
            }
                break;
            case Axis.Row:
            {
                if (slot.Fit != CrossFit.Fill)
                {
                    var offset = widget.Offset;
                    offset.Y = slot.Align switch
                    {
                        CrossAlign.Start => 0.0f,
                        CrossAlign.Center => (crossAxisSize / 2.0f) - (size.Y / 2.0f),
                        CrossAlign.End => size.Y - crossAxisSize,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    widget.Offset = (offset);
                }
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override ContainerSlot MakeSlot(Widget widget) => new ListSlot(this)
    {
        Child = widget,
    };
}