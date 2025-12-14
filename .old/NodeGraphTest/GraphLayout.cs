using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Rin.Framework.Shared.Math;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Layouts;

namespace NodeGraphTest;

public class GraphLayout : InfiniteChildrenLayout
{
    public override ICompositeView Container { get; }
    
    public GraphLayout(ICompositeView view)
    {
        Container = view;
    }
    public override ISlot MakeSlot(IView view)
    {
        return new GraphSlot
        {
            Position = Vector2.Zero,
            Child = view
        };
    }

    public override void OnSlotUpdated(ISlot slot)
    {
        Debug.Assert(slot is GraphSlot);
        LayoutSlot(Unsafe.As<GraphSlot>(slot));
    }

    public override Vector2 Apply(in Vector2 availableSpace)
    {
        foreach (var slot in GetSlots())
        {
            Debug.Assert(slot is GraphSlot);
            LayoutSlot(Unsafe.As<GraphSlot>(slot));
        }
        
        return availableSpace.FiniteOr();
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return Vector2.Zero;
    }

    public void LayoutSlot(GraphSlot slot)
    {
        slot.Child.Offset = slot.Position;
        slot.Child.ComputeSize(Vector2.PositiveInfinity);
    }
}