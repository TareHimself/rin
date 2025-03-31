using System.Numerics;
using Rin.Engine.Views.Composite;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Layouts;

public class FlexBoxSlot(FlexLayout? layout = null) : ListSlot(layout)
{
    public float? Flex = null;
}

public class FlexLayout : ListLayout
{
    public FlexLayout(Axis axis, CompositeView container) : base(axis, container)
    {
    }

    public override ISlot MakeSlot(View view)
    {
        return new FlexBoxSlot(this)
        {
            Child = view
        };
    }

    public override Vector2 Apply(Vector2 availableSpace)
    {
        var axis = GetAxis();
        float mainAxisAvailableSpace;
        Vector2 space;
        switch (axis)
        {
            case Axis.Column:
            {
                mainAxisAvailableSpace = availableSpace.Y;
                space = availableSpace with { Y = float.PositiveInfinity };
            }
                break;
            case Axis.Row:
            {
                mainAxisAvailableSpace = availableSpace.X;
                space = availableSpace with { X = float.PositiveInfinity };
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
        var flexTotal = 0.0f;
        var slots = GetSlots().ToArray();

        if (mainAxisAvailableSpace > 0.0f)
        {
            foreach (var slot in slots)
                if (slot is FlexBoxSlot { Flex: not null } asFlexSlot)
                {
                    flexTotal += asFlexSlot.Flex.Value;
                }
                else
                {
                    var viewSize = slot.Child.ComputeSize(axis switch
                    {
                        Axis.Column => space with { X = GetSlotCrossAxisSize(slot, space.X) },
                        Axis.Row => space with { Y = GetSlotCrossAxisSize(slot, space.Y) },
                        _ => throw new ArgumentOutOfRangeException()
                    });
                    mainAxisAvailableSpace -= axis switch
                    {
                        Axis.Column => viewSize.Y,
                        Axis.Row => viewSize.X,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }

            mainAxisAvailableSpace = Math.Max(mainAxisAvailableSpace, 0);
        }


        {
            var offset = 0.0f;
            // Compute slot sizes and initial offsets
            foreach (var slot in slots)
            {
                var view = slot.Child;
                switch (axis)
                {
                    case Axis.Column:
                        view.Offset = new Vector2(0, offset);
                        break;
                    case Axis.Row:
                        view.Offset = new Vector2(offset, 0);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                

                var slotMainAxisSize = 0.0f;
                var slotCrossAxisSize = 0.0f;
                if (slot is FlexBoxSlot { Flex: not null } asFlexSlot)
                {
                    var assignedMainAxisSpace =
                        flexTotal > 0.0f ? mainAxisAvailableSpace * (asFlexSlot.Flex.Value / flexTotal) : 0.0f;
                    Vector2 flexSize;
                    switch (axis)
                    {
                        case Axis.Column:
                        {
                            flexSize = new Vector2(GetSlotCrossAxisSize(slot, space.X),assignedMainAxisSpace);

                            slotMainAxisSize = flexSize.Y;
                            slotCrossAxisSize = flexSize.X;
                        }
                            break;
                        case Axis.Row:
                        {
                            flexSize = new Vector2(assignedMainAxisSpace, GetSlotCrossAxisSize(slot, space.Y));

                            slotMainAxisSize = flexSize.X;
                            slotCrossAxisSize = flexSize.Y;
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    
                    view.ComputeSize(flexSize);
                }
                else
                {
                    switch (axis)
                    {
                        case Axis.Column:
                        {
                            var viewSize = view.ComputeSize(space with { X = GetSlotCrossAxisSize(slot, space.X) });
                            slotMainAxisSize = viewSize.Y;
                            slotCrossAxisSize = viewSize.X;
                        }
                            break;
                        case Axis.Row:
                        {
                            var viewSize = view.ComputeSize(space with { Y = GetSlotCrossAxisSize(slot, space.Y) });
                            slotMainAxisSize = viewSize.X;
                            slotCrossAxisSize = viewSize.Y;
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                offset += slotMainAxisSize;
                mainAxisSize += slotMainAxisSize;
                crossAxisSize = Math.Max(crossAxisSize, slotCrossAxisSize);
            }

            crossAxisSize = float.IsFinite(space.Y) ? space.Y : crossAxisSize;
        }

        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot, crossAxisSize);
        }

        return axis switch
        {
            Axis.Column => new Vector2(crossAxisSize,mainAxisSize),
            Axis.Row => new Vector2(mainAxisSize, crossAxisSize),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected override Vector2 ArrangeContentRow(Vector2 availableSpace)
    {
        var mainAxisAvailableSpace = availableSpace.X.FiniteOr();
        var space = new Vector2(float.PositiveInfinity, availableSpace.Y);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
        var flexTotal = 0.0f;
        var slots = GetSlots().ToArray();

        if (mainAxisAvailableSpace > 0.0f)
        {
            foreach (var slot in slots)
                if (slot is FlexBoxSlot { Flex: not null } asFlexSlot)
                {
                    flexTotal += asFlexSlot.Flex.Value;
                }
                else
                {
                    var viewSize = slot.Child.ComputeSize(new Vector2(space.X, GetSlotCrossAxisSize(slot, space.Y)));
                    mainAxisAvailableSpace -= viewSize.X;
                }

            mainAxisAvailableSpace = Math.Max(mainAxisAvailableSpace, 0);
        }


        {
            var offset = new Vector2(0.0f);
            // Compute slot sizes and initial offsets
            foreach (var slot in slots)
            {
                var view = slot.Child;
                view.Offset = offset;

                var slotMainAxisSize = 0.0f;
                var slotCrossAxisSize = 0.0f;
                if (slot is FlexBoxSlot { Flex: not null } asFlexSlot)
                {
                    var assignedMainAxisSpace =
                        flexTotal > 0.0f ? mainAxisAvailableSpace * (asFlexSlot.Flex.Value / flexTotal) : 0.0f;

                    var flexSize = new Vector2(assignedMainAxisSpace, GetSlotCrossAxisSize(slot, space.Y));

                    view.ComputeSize(flexSize);

                    slotMainAxisSize = flexSize.X;
                    slotCrossAxisSize = flexSize.Y;
                }
                else
                {
                    var viewSize = view.ComputeSize(new Vector2(space.X, GetSlotCrossAxisSize(slot, space.Y)));
                    slotMainAxisSize = viewSize.X;
                    slotCrossAxisSize = viewSize.Y;
                }

                offset.X += slotMainAxisSize;
                mainAxisSize += slotMainAxisSize;
                crossAxisSize = Math.Max(crossAxisSize, slotCrossAxisSize);
            }

            crossAxisSize = float.IsFinite(space.Y) ? space.Y : crossAxisSize;
        }

        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot, crossAxisSize);
        }

        return new Vector2(mainAxisSize, crossAxisSize);
    }


    protected override Vector2 ArrangeContentColumn(Vector2 availableSpace)
    {
        var mainAxisAvailableSpace = availableSpace.Y.FiniteOr();
        var space = new Vector2(availableSpace.Y, float.PositiveInfinity);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
        var flexTotal = 0.0f;
        var slots = GetSlots().ToArray();

        if (mainAxisAvailableSpace > 0.0f)
        {
            foreach (var slot in slots)
                if (slot is FlexBoxSlot { Flex: not null } asFlexSlot)
                {
                    flexTotal += asFlexSlot.Flex.Value;
                }
                else
                {
                    var viewSize = slot.Child.ComputeSize(new Vector2(space.X, GetSlotCrossAxisSize(slot, space.Y)));
                    mainAxisAvailableSpace -= viewSize.X;
                }

            mainAxisAvailableSpace = Math.Max(mainAxisAvailableSpace, 0);
        }


        {
            var offset = new Vector2(0.0f);
            // Compute slot sizes and initial offsets
            foreach (var slot in slots)
            {
                var view = slot.Child;
                view.Offset = offset;

                var slotMainAxisSize = 0.0f;
                var slotCrossAxisSize = 0.0f;
                if (slot is FlexBoxSlot { Flex: not null } asFlexSlot)
                {
                    var assignedMainAxisSpace =
                        flexTotal > 0.0f ? mainAxisAvailableSpace * (asFlexSlot.Flex.Value / flexTotal) : 0.0f;

                    var flexSize = new Vector2(GetSlotCrossAxisSize(slot, space.Y), assignedMainAxisSpace);

                    view.ComputeSize(flexSize);

                    slotMainAxisSize = flexSize.Y;
                    slotCrossAxisSize = flexSize.X;
                }
                else
                {
                    var viewSize = view.ComputeSize(new Vector2(GetSlotCrossAxisSize(slot, space.Y), space.X));
                    slotMainAxisSize = viewSize.Y;
                    slotCrossAxisSize = viewSize.X;
                }

                offset.Y += slotMainAxisSize;
                mainAxisSize += slotMainAxisSize;
                crossAxisSize = Math.Max(crossAxisSize, slotCrossAxisSize);
            }

            crossAxisSize = float.IsFinite(space.X) ? space.X : crossAxisSize;
        }

        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot, crossAxisSize);
        }

        return new Vector2(mainAxisSize, crossAxisSize);
    }
}