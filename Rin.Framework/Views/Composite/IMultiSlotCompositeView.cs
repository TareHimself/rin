using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

public interface IMultiSlotCompositeView<in TSlotType> : ICompositeView where TSlotType : ISlot
{
    /// <summary>
    ///     Adds the views to this container
    /// </summary>
    public IView[] Children
    {
        init;
    }

    /// <summary>
    ///     Adds the slots to this container
    /// </summary>
    public TSlotType[] Slots
    {
        init;
    }

    public int SlotCount { get; }
    public abstract bool Add(IView child);
    public bool Add(TSlotType slot);
    public bool Remove(IView child);
}