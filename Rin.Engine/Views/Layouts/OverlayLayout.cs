using System.Numerics;
using Rin.Engine.Math;
using Rin.Engine.Views.Composite;

namespace Rin.Engine.Views.Layouts;

public class OverlayLayout(CompositeView container) : InfiniteChildrenLayout
{
    public override CompositeView Container { get; } = container;
    public override int MaxSlotCount => int.MaxValue;

    public override void Dispose()
    {
    }

    public override ISlot MakeSlot(View view)
    {
        return new Slot(this)
        {
            Child = view
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null) Apply(Container.GetContentSize());
    }

    public override Vector2 Apply(Vector2 availableSpace)
    {
        var dims = new Vector2(0.0f);

        // First pass is for views with content to figure out their size
        foreach (var slot in GetSlots())
        {
            var desiredSize = slot.Child.GetDesiredSize();
            if (desiredSize.X <= 0.0f || desiredSize.Y <= 0.0f) continue;
            var viewSize = slot.Child.ComputeSize(availableSpace);

            dims.X = float.Max(dims.X, viewSize.X.FiniteOr());
            dims.Y = float.Max(dims.Y, viewSize.Y.FiniteOr());
        }

        // Second pass is for views that adapt to the size of the container
        foreach (var slot in GetSlots())
        {
            slot.Child.Offset = default;
            slot.Child.ComputeSize(dims);
        }

        return new Vector2(float.Min(dims.X, availableSpace.X), float.Min(dims.Y, availableSpace.Y));
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return GetSlots().Aggregate(new Vector2(), (size, slot) =>
        {
            var slotSize = slot.Child.GetDesiredSize();
            size.Y = float.Max(size.Y, slotSize.Y);
            size.X = float.Max(size.X, slotSize.X);
            return size;
        });
    }
}