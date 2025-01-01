using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

public class RootLayout(CompositeView container) : InfiniteChildrenLayout
{
    public override CompositeView Container { get; } = container;

    public override void Dispose()
    {
        
    }

    public override ISlot MakeSlot(View widget)
    {
        return new Slot(this)
        {
            Child = widget
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null)
        {
            slot.Child.Offset = 0.0f;
            slot.Child.ComputeSize(Container.GetContentSize());
        }
    }

    public override Vec2<float> Apply(Vec2<float> availableSpace)
    {
        foreach (var slot in GetSlots())
        {
            slot.Child.Offset = 0.0f;
            slot.Child.ComputeSize(availableSpace);
        };
        
        return availableSpace;
    }

    public override Vec2<float> ComputeDesiredContentSize()
    {
        return 0.0f;
    }
}