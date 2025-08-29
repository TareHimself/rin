using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

public abstract class MultiSlotCompositeView<TSlotType> : CompositeView ,IMultiSlotCompositeView<TSlotType>  where TSlotType : ISlot
{

    public IView[] Children
    {
        init
        {
            foreach (var child in value) Add(child);
        }
    }
    
    public TSlotType[] Slots
    {
        init
        {
            foreach (var slot in value) Add(slot);
        }
    }

    public abstract int SlotCount { get; }
    public abstract bool Add(IView child);
    public abstract bool Add(TSlotType slot);
    public abstract bool Remove(IView child);
}