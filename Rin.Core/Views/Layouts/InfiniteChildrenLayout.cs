using System.Numerics;
using Rin.Core.Views.Composite;

namespace Rin.Core.Views.Layouts;

public abstract class InfiniteChildrenLayout : IMultiSlotLayout
{
    protected readonly Dictionary<IView, ISlot> SlotMap = [];
    protected readonly List<ISlot> Slots = [];

    public virtual int MaxSlotCount => int.MaxValue;
    public int SlotCount => Slots.Count;
    public abstract ICompositeView Container { get; }

    public bool Add(IView child)
    {
        return Add(MakeSlot(child));
    }

    public virtual bool Add(ISlot slot)
    {
        var added = false;

        if (Slots.Count != MaxSlotCount)
        {
            var view = slot.Child;
            Slots.Add(slot);
            SlotMap.TryAdd(view, slot);
            added = true;
        }

        if (added)
        {
            slot.Child.SetParent(Container);
            slot.OnAddedToLayout(this);
            Container.OnChildAdded(slot.Child);
        }

        return added;
    }

    public virtual bool Remove(IView view)
    {
        var removed = false;

        for (var i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].Child != view) continue;
            Slots[i].OnRemovedFromLayout(this);
            Slots.RemoveAt(i);
            SlotMap.Remove(view);
            removed = true;
            break;
        }

        if (removed)
        {
            view.SetParent(null);
            Container.OnChildRemoved(view);
        }

        return removed;
    }

    public abstract ISlot MakeSlot(IView view);

    public virtual ISlot? GetSlot(int idx)
    {
        return Slots[idx];
    }

    public virtual ISlot[] GetSlots()
    {
        return Slots.ToArray();
    }

    public abstract void OnSlotUpdated(ISlot slot);

    public abstract Vector2 Apply(in Vector2 availableSpace);

    public abstract Vector2 ComputeDesiredContentSize();

    public ISlot? FindSlot(IView view)
    {
        return SlotMap.GetValueOrDefault(view);
    }
    
    
}