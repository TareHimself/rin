using System.Numerics;
using Rin.Framework.Views.Composite;

namespace Rin.Framework.Views.Layouts;

public class RootLayout(ICompositeView container) : InfiniteChildrenLayout
{
    public override ICompositeView Container { get; } = container;

    public override ISlot MakeSlot(IView view)
    {
        return new Slot(this)
        {
            Child = view
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null)
        {
            slot.Child.Offset = default;
            slot.Child.ComputeSize(Container.GetContentSize());
        }
    }

    public override Vector2 Apply(in Vector2 availableSpace)
    {
        foreach (var slot in GetSlots())
        {
            slot.Child.Offset = default;
            slot.Child.ComputeSize(availableSpace);
        }

        ;

        return availableSpace;
    }

    public override Vector2 ComputeDesiredContentSize() => Vector2.Zero;
}