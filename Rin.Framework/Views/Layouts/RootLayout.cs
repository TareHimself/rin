using System.Numerics;
using Rin.Framework.Views.Composite;

namespace Rin.Framework.Views.Layouts;

public class RootLayout(CompositeView container) : InfiniteChildrenLayout
{
    public override CompositeView Container { get; } = container;

    public override ISlot MakeSlot(View view)
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

    public override Vector2 Apply(Vector2 availableSpace)
    {
        foreach (var slot in GetSlots())
        {
            slot.Child.Offset = default;
            slot.Child.ComputeSize(availableSpace);
        }

        ;

        return availableSpace;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }
}