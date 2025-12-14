using System.Diagnostics;
using System.Numerics;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Layouts;

namespace NodeGraphTest;

public class GraphNodeLayout : ILayout
{

    private List<GraphNodeSlot> _inputs = [];
    private List<GraphNodeSlot> _outputs = [];
    
    public GraphNodeLayout(ICompositeView container)
    {
        Container = container;
    }

    public IEnumerable<GraphNodeSlot> GetSlots()
    {
        foreach (var slot in _outputs)
        {
            yield return slot;
        }
        
        foreach (var slot in _inputs)
        {
            yield return slot;
        }
    }
    public ICompositeView Container { get; }
    
    public ISlot MakeSlot(IView view)
    {
        Debug.Assert(view is IGraphPinView);
        
        return new GraphNodeSlot
        {
            Child = view
        };
    }

    public void OnSlotUpdated(ISlot slot)
    {
        if (Container.Surface != null) Apply(Container.GetContentSize());
    }
    
    public Vector2 Apply(in Vector2 availableSpace)
    {
        var size = Vector2.Zero;
        foreach (var slot in GetSlots())
        {
            slot.Child.Offset = slot.Child.Offset with{ Y = size.Y };
            var slotSize = slot.Child.ComputeSize(availableSpace);
            size = size with{ X = float.Max(slotSize.X,size.X), Y = size.Y + slotSize.Y };
        }
        foreach (var slot in _outputs)
        {
            var slotSize = slot.Child.GetSize();
            slot.Child.Offset = slot.Child.Offset with{ X = size.X - slotSize.X };
        }
        return size;
    }

    public Vector2 ComputeDesiredContentSize()
    {
        return GetSlots().Aggregate(new Vector2(0), (size, slot) =>
        {
            var desired = slot.Child.GetDesiredSize();
            return size with{ X = float.Max(desired.X,size.X),Y = desired.Y + size.Y };
        });
    }
}