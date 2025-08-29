using System.Numerics;
using Rin.Framework.Views.Composite;

namespace Rin.Framework.Views.Layouts;

public abstract class InfiniteChildrenLayout : IMultiSlotLayout
{
    private readonly Dictionary<IView, ISlot> _slotMap = [];
    private readonly List<ISlot> _slots = [];

    public virtual int MaxSlotCount => int.MaxValue;
    public int SlotCount => _slots.Count;
    public abstract ICompositeView Container { get; }

    public bool Add(IView child)
    {
        return Add(MakeSlot(child));
    }

    public bool Add(ISlot slot)
    {
        var added = false;

        // lock (_slots)
        // {
        if (_slots.Count != MaxSlotCount)
        {
            var view = slot.Child;
            _slots.Add(slot);
            _slotMap.TryAdd(view, slot);
            added = true;
        }
        // }

        if (added)
        {
            slot.Child.SetParent(Container);
            slot.OnAddedToLayout(this);
            Container.OnChildAdded(slot.Child);
        }

        return added;
    }

    public bool Remove(IView view)
    {
        var removed = false;

        // lock (_slots)
        // {
        for (var i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].Child != view) continue;
            _slots[i].OnRemovedFromLayout(this);
            _slots.RemoveAt(i);
            _slotMap.Remove(view);
            //_slotMap.TryRemove(view, out var _);
            removed = true;
            break;
        }
        // }

        if (removed)
        {
            view.SetParent(null);
            Container.OnChildRemoved(view);
        }

        return removed;
    }

    public abstract ISlot MakeSlot(IView view);

    public ISlot? GetSlot(int idx)
    {
        // lock (_slots)
        // {
        return _slots[idx];
        // }
    }

    public IEnumerable<ISlot> GetSlots()
    {
        // lock (_slots)
        // {
        return _slots.ToArray();
        //}
    }

    public abstract void OnSlotUpdated(ISlot slot);

    public abstract Vector2 Apply(in Vector2 availableSpace);
    
    public abstract Vector2 ComputeDesiredContentSize();

    public ISlot? FindSlot(IView view)
    {
        return _slotMap.GetValueOrDefault(view);
    }
}