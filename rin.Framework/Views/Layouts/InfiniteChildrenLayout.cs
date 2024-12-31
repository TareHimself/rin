using System.Collections.Concurrent;
using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

public abstract class InfiniteChildrenLayout : IMultiSlotLayout
{
    protected readonly ConcurrentDictionary<View, ISlot> _widgetSlotMap = [];
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
                _widgetSlotMap.TryAdd(view, slot);
                added = true;
            }
        }

        if (added)
        {
            slot.Child.SetParent(Container);
            slot.SetLayout(this);
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
                _slots.RemoveAt(i);
                _widgetSlotMap.TryRemove(view, out var _);
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

    public abstract ISlot MakeSlot(View widget);

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

    public abstract Vector2<float> Apply(Vector2<float> availableSpace);
    public abstract Vector2<float> ComputeDesiredContentSize();

    public ISlot? FindSlot(View view)
    {
        lock (_slots)
        {
            if (_widgetSlotMap.TryGetValue(view, out var value))
            {
                return value;
            }
        }

        return null;
    }
}