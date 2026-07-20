using Rin.Core.Views.Layouts;

namespace Rin.Core.Views.Composite;

public interface IMultiSlotCompositeView<in TSlotType> : ICompositeView where TSlotType : ISlot
{
    /// <summary>
    ///     Adds the views to this container
    /// </summary>
    public IView[] InitChildren { init; }

    /// <summary>
    ///     Adds the slots to this container
    /// </summary>
    public TSlotType[] InitSlots { init; }

    public int SlotCount { get; }
    public bool Add(IView child);
    public bool Add(TSlotType slot);
    public bool Remove(IView child);
}