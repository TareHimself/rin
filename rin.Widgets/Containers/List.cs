using rin.Core.Math;

namespace rin.Widgets.Containers;


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

public class ListContainerSlot(List? container = null) : ContainerSlot(container)
{
    public CrossFit Fit = CrossFit.Desired;
    public CrossAlign Align = CrossAlign.Start;
}

/// <summary>
/// A container that draws children left to right
/// Slot = <see cref="ListContainerSlot"/>
/// </summary>
public class List(Axis axis) : Container
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
        TryUpdateDesiredSize();
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


    protected override void ArrangeSlots(Vector2<float> drawSize)
    {
        var offset = new Vector2<float>(0.0f);
        switch (axis)
        {
            case Axis.Row:
            {
                foreach (var slot in GetSlots())
                {
                    if (slot is not ListContainerSlot asListContainerSlot) continue;
                    var widget = slot.Child;
                    var widgetSize = widget.GetDesiredSize();
                    widget.Offset = (offset.Clone());
                    widget.Size = (new Vector2<float>
                    {
                        X = widgetSize.X,
                        Y = widgetSize.Y
                    });
                    HandleCrossAxis(asListContainerSlot,drawSize);
                    offset.X += widgetSize.X;
                }
                break;
            }
            case Axis.Column:
                
                {
                    foreach (var slot in GetSlots())
                    {
                        if (slot is not ListContainerSlot asListContainerSlot) continue;
                        var widget = slot.Child;
                        widget.Offset = (offset.Clone());
                        var widgetSize = widget.GetDesiredSize();
                        widget.Size = (new Vector2<float>
                        {
                            Y = widgetSize.Y,
                            X = widgetSize.X
                        });
                        HandleCrossAxis(asListContainerSlot,drawSize);
                        offset.Y += widgetSize.Y;
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void OnSlotUpdated(ContainerSlot slot)
    {
        TryUpdateDesiredSize();
        ArrangeSlots(GetContentSize());
    }

    protected virtual void HandleCrossAxis(ListContainerSlot slot,Vector2<float> drawSize)
    {
        var widget = slot.Child;
        var size = widget.Size;
        var desiredSize = widget.GetDesiredSize();
        switch (Axis)
        {
            case Axis.Column:
            {
                widget.Size = (new Vector2<float>
                {
                    X = slot.Fit switch
                    {
                        CrossFit.Desired => size.X,
                        CrossFit.Fill => drawSize.X,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    Y = size.Y
                });
                
                if (slot.Fit != CrossFit.Fill)
                {
                    size = widget.Size;
                    var offset = widget.Offset;
                    offset.X = slot.Align switch
                    {
                        CrossAlign.Start => 0.0f,
                        CrossAlign.Center => (drawSize.X / 2.0f) - (size.X / 2.0f),
                        CrossAlign.End => size.X - drawSize.X,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    widget.Offset = (offset);
                }
            }
                break;
            case Axis.Row:
            {
                widget.Size = (new Vector2<float>
                {
                    X = size.X,
                    Y = slot.Fit switch
                    {
                        CrossFit.Desired => size.Y,
                        CrossFit.Fill => drawSize.Y,
                        _ => throw new ArgumentOutOfRangeException()
                    }
                });

                if (slot.Fit != CrossFit.Fill)
                {
                    size = widget.Size;
                    var offset = widget.Offset;
                    offset.Y = slot.Align switch
                    {
                        CrossAlign.Start => 0.0f,
                        CrossAlign.Center => (drawSize.Y / 2.0f) - (size.Y / 2.0f),
                        CrossAlign.End => size.Y - drawSize.Y,
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

    protected override ContainerSlot MakeSlot(Widget widget) => new ListContainerSlot(this)
    {
        Child = widget,
    };
}