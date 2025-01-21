using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

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

public class ListSlot(ListLayout? layout = null) : Slot(layout)
{
    public CrossFit Fit = CrossFit.Desired;
    public CrossAlign Align = CrossAlign.Start;
}

public class ListLayout(Axis axis,CompositeView container) : InfiniteChildrenLayout
{
    private Axis _axis = axis;
    public override CompositeView Container { get; } = container;
    
    public override void Dispose()
    {
        
    }

    public override ISlot MakeSlot(View view)
    {
        return new ListSlot(this)
        {
            Child = view
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null)
        {
            Apply(Container.GetContentSize());
        }
    }
    
    protected virtual float GetSlotCrossAxisSize(ISlot slot, float crossAxisAvailableSize)
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
        var view = slot.Child;
        var size = view.Size;
        switch (GetAxis())
        {
            case Axis.Column:
            {
                if (slot.Fit != CrossFit.Fill)
                {
                    var offset = view.Offset;
                    offset.X = slot.Align switch
                    {
                        CrossAlign.Start => 0.0f,
                        CrossAlign.Center => (crossAxisSize / 2.0f) - (size.X / 2.0f),
                        CrossAlign.End => size.X - crossAxisSize,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    view.Offset = (offset);
                }
            }
                break;
            case Axis.Row:
            {
                if (slot.Fit != CrossFit.Fill)
                {
                    var offset = view.Offset;
                    offset.Y = slot.Align switch
                    {
                        CrossAlign.Start => 0.0f,
                        CrossAlign.Center => (crossAxisSize / 2.0f) - (size.Y / 2.0f),
                        CrossAlign.End => size.Y - crossAxisSize,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    view.Offset = (offset);
                }
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    protected virtual Vec2<float> ArrangeContentRow(Vec2<float> availableSpace)
    {
        var offset = new Vec2<float>(0.0f);
        
        var space = new Vec2<float>(float.PositiveInfinity,availableSpace.Y);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
        var slots = GetSlots().ToArray();
        
        foreach (var slot in slots)
        {
            var view = slot.Child;
            view.Offset = offset;
                    
            var viewSize = view.ComputeSize(new Vec2<float>(space.X,GetSlotCrossAxisSize(slot,space.Y)));
                    
            offset.X += viewSize.X;
            mainAxisSize += viewSize.X;
            crossAxisSize = Math.Max(crossAxisSize, viewSize.Y);
        }

        crossAxisSize = float.IsFinite(space.Y) ? space.Y : crossAxisSize;
                
        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot,crossAxisSize);
        }

        return new Vec2<float>(mainAxisSize, crossAxisSize);
    }
    
    protected virtual Vec2<float> ArrangeContentColumn(Vec2<float> availableSpace)
    {
        var offset = new Vec2<float>(0.0f);
        
        var space = new Vec2<float>(availableSpace.X,float.PositiveInfinity);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
                    
        var slots = GetSlots().ToArray();
                
        // Compute slot sizes and initial offsets
        foreach (var slot in slots)
        {
            var view = slot.Child;
            view.Offset = offset;
                    
            var viewSize = view.ComputeSize(new Vec2<float>(GetSlotCrossAxisSize(slot,space.X),space.Y));
                    
            offset.Y += viewSize.Y;
            mainAxisSize += viewSize.Y;
            crossAxisSize = Math.Max(crossAxisSize, viewSize.X);
        }

        crossAxisSize = float.IsFinite(space.X) ? space.X : crossAxisSize;
                
        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot,crossAxisSize);
        }

        return new Vec2<float>(crossAxisSize,mainAxisSize);
    }

    
    public override Vec2<float> Apply(Vec2<float> availableSpace)
    {
        return _axis switch
        {
            Axis.Row => ArrangeContentRow(availableSpace),
            Axis.Column => ArrangeContentColumn(availableSpace),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public Axis GetAxis() => _axis;

    public void SetAxis(Axis axis)
    {
        _axis = axis;
    }
    public override Vec2<float> ComputeDesiredContentSize()
    {
        return GetAxis() switch
        {
            Axis.Row => GetSlots().Aggregate(new Vec2<float>(), (size, slot) =>
            {
                var slotSize = slot.Child.GetDesiredSize();
                size.X += slotSize.X;
                size.Y = System.Math.Max(size.Y, slotSize.Y);
                return size;
            }),
            Axis.Column => GetSlots().Aggregate(new Vec2<float>(), (size, slot) =>
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