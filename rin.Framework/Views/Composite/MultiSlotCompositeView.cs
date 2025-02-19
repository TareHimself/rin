using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

public abstract class MultiSlotCompositeView<TSlotType> : CompositeView where TSlotType : ISlot
{
    /// <summary>
    ///     Adds the views to this container
    /// </summary>
    public View[] Children
    {
        init
        {
            foreach (var child in value) Add(child);
        }
    }

    /// <summary>
    ///     Adds the slots to this container
    /// </summary>
    public TSlotType[] Slots
    {
        init
        {
            foreach (var slot in value) Add(slot);
        }
    }

    public abstract int SlotCount { get; }
    public abstract bool Add(View child);
    public abstract bool Add(TSlotType slot);
    public abstract bool Remove(View child);
}