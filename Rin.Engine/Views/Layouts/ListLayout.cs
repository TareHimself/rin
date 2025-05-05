using System.Numerics;
using Rin.Engine.Views.Composite;

namespace Rin.Engine.Views.Layouts;

public enum Axis
{
    Column,
    Row
}

public class ListSlot(ListLayout? layout = null) : Slot(layout)
{
    public CrossAlign Align = CrossAlign.Start;
    public CrossFit Fit = CrossFit.Desired;
}

public class ListLayout(Axis axis, CompositeView container) : InfiniteChildrenLayout
{
    private Axis _axis = axis;
    public override CompositeView Container { get; } = container;

    public override ISlot MakeSlot(View view)
    {
        return new ListSlot(this)
        {
            Child = view
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null) Apply(Container.GetContentSize());
    }

    protected virtual float GetSlotCrossAxisSize(ISlot slot, float crossAxisAvailableSize)
    {
        if (slot is ListSlot asListContainerSlot)
            return asListContainerSlot.Fit switch
            {
                CrossFit.Desired => float.Clamp(asListContainerSlot.Child.GetDesiredSize().X, 0.0f,
                    crossAxisAvailableSize),
                CrossFit.Available => crossAxisAvailableSize,
                CrossFit.Fill => crossAxisAvailableSize,
                _ => throw new ArgumentOutOfRangeException()
            };

        return 0.0f;
    }

    /// <summary>
    ///     Assumes the slot is at the start of the cross axis
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="crossAxisSize"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    protected virtual void HandleCrossAxisOffset(ListSlot slot, float crossAxisSize)
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
                    offset.X += slot.Align switch
                    {
                        CrossAlign.Start => 0.0f,
                        CrossAlign.Center => crossAxisSize / 2.0f - size.X / 2.0f,
                        CrossAlign.End => size.X - crossAxisSize,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    view.Offset = offset;
                }
            }
                break;
            case Axis.Row:
            {
                if (slot.Fit != CrossFit.Fill)
                {
                    var offset = view.Offset;
                    offset.Y += slot.Align switch
                    {
                        CrossAlign.Start => 0.0f,
                        CrossAlign.Center => crossAxisSize / 2.0f - size.Y / 2.0f,
                        CrossAlign.End => size.Y - crossAxisSize,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    view.Offset = offset;
                }
            }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected virtual Vector2 ArrangeContentRow(Vector2 availableSpace)
    {
        var offset = new Vector2(0.0f);

        var space = new Vector2(float.PositiveInfinity, availableSpace.Y);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
        var slots = GetSlots().ToArray();

        foreach (var slot in slots)
        {
            var view = slot.Child;
            view.Offset = offset;

            var viewSize = view.ComputeSize(new Vector2(space.X, GetSlotCrossAxisSize(slot, space.Y)));

            offset.X += viewSize.X;
            mainAxisSize += viewSize.X;
            crossAxisSize = float.Max(crossAxisSize, viewSize.Y);
        }

        crossAxisSize = float.IsFinite(space.Y) ? space.Y : crossAxisSize;

        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot, crossAxisSize);
        }

        return new Vector2(mainAxisSize, crossAxisSize);
    }

    protected virtual Vector2 ArrangeContentColumn(Vector2 availableSpace)
    {
        var offset = new Vector2(0.0f);

        var space = new Vector2(availableSpace.X, float.PositiveInfinity);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;

        var slots = GetSlots().ToArray();

        // Compute slot sizes and initial offsets
        foreach (var slot in slots)
        {
            var view = slot.Child;
            view.Offset = offset;

            var viewSize = view.ComputeSize(new Vector2(GetSlotCrossAxisSize(slot, space.X), space.Y));

            offset.Y += viewSize.Y;
            mainAxisSize += viewSize.Y;
            crossAxisSize = float.Max(crossAxisSize, viewSize.X);
        }

        crossAxisSize = float.IsFinite(space.X) ? space.X : crossAxisSize;

        // Handle cross axis offsets (we could also handle main axis offsets here in the future)
        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot, crossAxisSize);
        }

        return new Vector2(crossAxisSize, mainAxisSize);
    }


    public override Vector2 Apply(Vector2 availableSpace)
    {
        return _axis switch
        {
            Axis.Row => ArrangeContentRow(availableSpace),
            Axis.Column => ArrangeContentColumn(availableSpace),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public Axis GetAxis()
    {
        return _axis;
    }

    public void SetAxis(Axis axis)
    {
        _axis = axis;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return GetAxis() switch
        {
            Axis.Row => GetSlots().Aggregate(new Vector2(), (size, slot) =>
            {
                var slotSize = slot.Child.GetDesiredSize();
                size.X += slotSize.X;
                size.Y = float.Max(size.Y, slotSize.Y);
                return size;
            }),
            Axis.Column => GetSlots().Aggregate(new Vector2(), (size, slot) =>
            {
                var slotSize = slot.Child.GetDesiredSize();
                size.Y += slotSize.Y;
                size.X = float.Max(size.X, slotSize.X);
                return size;
            }),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}