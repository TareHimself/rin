using System.Collections.Concurrent;
using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

public abstract class InfiniteChildrenLayout : IMultiSlotLayout
{
    protected readonly ConcurrentDictionary<View, ISlot> _viewSlotMap = [];
    protected readonly List<ISlot> _slots = [];

    public virtual int MaxSlotCount => int.MaxValue;
    public int SlotCount {
        get
        {
            lock (_slots)
            {
                return _slots.Count;
            }
        }
    }
    public abstract CompositeView Container { get; }

    public abstract void Dispose();
    
    public bool Add(View child)
    {
        return Add(MakeSlot(child));
    }

    public bool Add(ISlot slot)
    {
        var added = false;
        
        lock (_slots)
        {
            if (_slots.Count != MaxSlotCount)
            {
                var view = slot.Child;
                _slots.Add(slot);
                _viewSlotMap.TryAdd(view, slot);
                added = true;
            }
        }

        if (added)
        {
            slot.Child.SetParent(Container);
            slot.OnAddedToLayout(this);
            Container.OnChildAdded(slot.Child);
        }
        
        return added;
    }

    public bool Remove(View view)
    {
        var removed = false;
        
        lock (_slots)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Child != view) continue;
                _slots[i].OnRemovedFromLayout(this);
                _slots.RemoveAt(i);
                _viewSlotMap.TryRemove(view, out var _);
                removed = true;
                break;
            }
        }

        if (removed)
        {
            view.SetParent(null);
            Container.OnChildRemoved(view);
        }
        
        return removed;
    }

    public abstract ISlot MakeSlot(View view);

    public ISlot? GetSlot(int idx)
    {
        lock (_slots)
        {
            return _slots[idx];
        }
    }

    public IEnumerable<ISlot> GetSlots()
    {
        lock (_slots)
        {
            return _slots.ToArray();
        }
    }

    public abstract void OnSlotUpdated(ISlot slot);

    public abstract Vector2 Apply(Vector2 availableSpace);
    public abstract Vector2 ComputeDesiredContentSize();

    public ISlot? FindSlot(View view)
    {
        lock (_slots)
        {
            if (_viewSlotMap.TryGetValue(view, out var value))
            {
                return value;
            }
        }

        return null;
    }
}