using System.Numerics;
using Rin.Core.Views.Composite;

namespace Rin.Core.Views.Layouts;

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

public class ListLayout(Axis axis, ICompositeView container) : InfiniteChildrenLayout
{
    private Axis _axis = axis;
    public override ICompositeView Container { get; } = container;

    public override ISlot MakeSlot(IView view)
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
                CrossFit.Desired => float.Clamp(
                    GetAxis() == Axis.Row
                        ? asListContainerSlot.Child.GetDesiredSize().Y
                        : asListContainerSlot.Child.GetDesiredSize().X,
                    0.0f, crossAxisAvailableSize),
                CrossFit.Available => crossAxisAvailableSize,
                CrossFit.Fill => crossAxisAvailableSize,
                _ => throw new ArgumentOutOfRangeException()
            };

        return 0.0f;
    }

    /// <summary>
    ///     Assumes the slot is at the start of the cross axis
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    protected virtual void HandleCrossAxisOffset(ListSlot slot, float crossAxisSize)
    {
        var view = slot.Child;
        var axis = GetAxis();
        var crossSize = axis switch
        {
            Axis.Column => view.GetSize().X,
            Axis.Row => view.GetSize().Y,
            _ => throw new ArgumentOutOfRangeException()
        };
        var crossOffset = slot.Align switch
        {
            CrossAlign.Start => 0.0f,
            CrossAlign.Center => crossAxisSize / 2.0f - crossSize / 2.0f,
            CrossAlign.End => crossAxisSize - crossSize,
            _ => throw new ArgumentOutOfRangeException()
        };
        view.Offset += axis == Axis.Column ? new Vector2(crossOffset, 0.0f) : new Vector2(0.0f, crossOffset);
    }

    protected virtual Vector2 ArrangeContentRow(Vector2 availableSpace)
    {
        var offset = new Vector2(0.0f);

        var space = new Vector2(float.PositiveInfinity, availableSpace.Y);
        var mainAxisSize = 0.0f;
        var crossAxisSize = 0.0f;
        var slots = GetSlots();

        foreach (var slot in slots)
        {
            var view = slot.Child;
            view.Offset = offset;

            var viewSize = view.Layout(new Vector2(space.X, GetSlotCrossAxisSize(slot, space.Y)));

            offset.X += viewSize.X;
            mainAxisSize += viewSize.X;
            crossAxisSize = float.Max(crossAxisSize, viewSize.Y);
        }

        crossAxisSize = float.IsFinite(space.Y) ? space.Y : crossAxisSize;

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

        var slots = GetSlots();

        foreach (var slot in slots)
        {
            var view = slot.Child;
            view.Offset = offset;

            var viewSize = view.Layout(new Vector2(GetSlotCrossAxisSize(slot, space.X), space.Y));

            offset.Y += viewSize.Y;
            mainAxisSize += viewSize.Y;
            crossAxisSize = float.Max(crossAxisSize, viewSize.X);
        }

        crossAxisSize = float.IsFinite(space.X) ? space.X : crossAxisSize;

        foreach (var slot in slots)
        {
            if (slot is not ListSlot asListContainerSlot) continue;
            HandleCrossAxisOffset(asListContainerSlot, crossAxisSize);
        }

        return new Vector2(crossAxisSize, mainAxisSize);
    }

    public override Vector2 Apply(in Vector2 availableSpace)
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
        var axis = GetAxis();
        if (axis is not (Axis.Row or Axis.Column)) throw new ArgumentOutOfRangeException();

        var size = new Vector2();
        foreach (var slot in GetSlots())
        {
            var slotSize = slot.Child.GetDesiredSize();
            if (axis == Axis.Row)
            {
                size.X += slotSize.X;
                size.Y = float.Max(size.Y, slotSize.Y);
            }
            else
            {
                size.Y += slotSize.Y;
                size.X = float.Max(size.X, slotSize.X);
            }
        }

        return size;
    }
}