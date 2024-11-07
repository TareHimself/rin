using System.Collections.Concurrent;
using rin.Core.Extensions;
using rin.Core.Math;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Commands;

namespace rin.Widgets;
public abstract class Container : Widget
{
    private readonly ConcurrentDictionary<Widget, ContainerSlot> _widgetSlotMap = [];
    private readonly List<ContainerSlot> _slots = [];
    
    protected readonly Mutex SlotsMutex = new();
    
    public Clip Clip = Clip.None;
    
    
    /// <summary>
    /// Adds the Widget to this container
    /// </summary>
    public Widget Child
    {
        init => AddChild(value);
    }
    
    /// <summary>
    /// Adds the widgets to this container
    /// </summary>
    public Widget[] Children
    {
        init
        {
            foreach (var slot in value)
            {
                AddChild(slot);
            }
        }
    }
    
    /// <summary>
    /// Adds the slot to this container
    /// </summary>
    public ContainerSlot Slot
    {
        init => AddChild(value);
    }

    
    /// <summary>
    /// Adds the slots to this container
    /// </summary>
    public ContainerSlot[] Slots
    {
        init
        {
            foreach (var slot in value)
            {
                AddChild(slot);
            }
        }
    }


    public override Vector2<float> Size
    {
        set
        {
            base.Size = value;
            ArrangeSlots(GetContentSize());
        }
    }

    protected abstract void ArrangeSlots(Vector2<float> drawSize);


    protected virtual ContainerSlot MakeSlot(Widget widget)
    {
        return new ContainerSlot(this)
        {
            Child = widget
        };
    }
    
    public virtual ContainerSlot? AddChild<TE>() where TE : Widget
    {
        return AddChild(Activator.CreateInstance<TE>());
    }

    public void OnSlotUpdated(Widget widget)
    {
        if (_widgetSlotMap.TryGetValue(widget, out var slot))
        {
            OnSlotUpdated(slot);
        }
    }
    public virtual void OnSlotUpdated(ContainerSlot slot)
    {
        if (TryUpdateDesiredSize()) ArrangeSlots(GetContentSize());
    }

    public ContainerSlot? AddChild(Widget widget) => AddChild(MakeSlot(widget));
    
    public ContainerSlot? AddChild(ContainerSlot slot)
    {
        lock (SlotsMutex)
        {
            var maxSlots = GetMaxSlotsCount();
            if (maxSlots > 0 && _slots.Count == maxSlots) return null;
                
            var widget = slot.Child;
            
            widget.SetParent(this);
            
            slot.SetOwner(this);
            
            _slots.Add(slot);
            _widgetSlotMap.TryAdd(widget,slot);

            if (Surface != null)
            {
                widget.NotifyAddedToSurface(Surface);
                OnSlotUpdated(slot);
            }
            //Console.WriteLine("Added child [{0}] to container [{1}]", widget.GetType().Name, GetType().Name);
            return slot;
        }
    }

    public virtual bool RemoveChild(Widget widget)
    {
        lock (SlotsMutex)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Child != widget) continue;

                widget.SetParent(null);

                if (Surface != null)
                {
                    widget.NotifyRemovedFromSurface(Surface);
                }

                _slots.RemoveAt(i);
                _widgetSlotMap.TryRemove(widget,out var _);

                if (Surface != null)
                {
                    TryUpdateDesiredSize();
                }
                return true;
            }

            return false;
        }
    }

    public virtual ContainerSlot? GetSlot(int idx)
    {
        lock (SlotsMutex)
        {
            return idx < _slots.Count ? _slots[idx] : null;
        }
    }

    public virtual ContainerSlot[] GetSlots()
    {
        lock (SlotsMutex)
        {
            return _slots.ToArray();
        }
    }

    public virtual int GetSlotsCount()
    {
        lock (SlotsMutex)
        {
            return _slots.Count;
        }
    }

    public virtual int GetMaxSlotsCount() => int.MaxValue;

    public override Widget? ReceiveCursorDown(CursorDownEvent e, TransformInfo info)
    {
        if (IsChildrenHitTestable)
        {
            var res = ChildrenReceiveCursorDown(e, info);
            if (res != null ) return res;
        }

        return base.ReceiveCursorDown(e, info);
    }

    protected virtual Widget? ChildrenReceiveCursorDown(CursorDownEvent e, TransformInfo info)
    {
        var point = e.Position.Cast<float>();
        foreach (var slot in GetHitTestableSlots().ToArray().AsReversed())
        {
            var slotInfo = ComputeContentTransform(slot, info);
            if (!slotInfo.PointWithin(point)) continue;
            var res = slot.Child.ReceiveCursorDown(e, slotInfo);
            if (res != null) return res;
        }

        return null;
    }

    public override void ReceiveCursorEnter(CursorMoveEvent e, TransformInfo info, List<Widget> items)
    {
        if (IsChildrenHitTestable) ChildrenReceiveCursorEnter(e, info, items);

        base.ReceiveCursorEnter(e, info, items);
    }

    protected virtual void ChildrenReceiveCursorEnter(CursorMoveEvent e, TransformInfo info, List<Widget> items)
    {
        var point = e.Position.Cast<float>();
            foreach (var slot in GetHitTestableSlots())
            {
                var slotInfo = ComputeContentTransform(slot, info);
                if (slotInfo.PointWithin(point))
                    slot.Child.ReceiveCursorEnter(e, slotInfo, items);
            }
    }

    public override bool ReceiveCursorMove(CursorMoveEvent e, TransformInfo info)
    {
        if (IsChildrenHitTestable && ChildrenReceiveCursorMove(e, info)) return true;

        return base.ReceiveCursorMove(e, info);
    }

    protected virtual bool ChildrenReceiveCursorMove(CursorMoveEvent e, TransformInfo info)
    {
        var point = e.Position.Cast<float>();
        foreach (var slot in GetHitTestableSlots())
        {
            var slotInfo = ComputeContentTransform(slot, info);
            if (slotInfo.PointWithin(point) &&
                slot.Child.ReceiveCursorMove(e, slotInfo))
                return true;
        }

        return false;
    }

    public override bool ReceiveScroll(ScrollEvent e, TransformInfo info)
    {
        if (IsChildrenHitTestable && ChildrenReceiveScroll(e, info)) return true;

        return base.ReceiveScroll(e, info);
    }

    protected virtual bool ChildrenReceiveScroll(ScrollEvent e, TransformInfo info)
    {
        var point = e.Position.Cast<float>();
            foreach (var slot in GetHitTestableSlots())
            {
                var slotInfo = ComputeContentTransform(slot, info);
                if (slotInfo.PointWithin(point) &&
                    slot.Child.ReceiveScroll(e, slotInfo))
                    return true;
            }

        return false;
    }

    public override void NotifyAddedToSurface(Surface surface)
    {
        base.NotifyAddedToSurface(surface);
        lock (SlotsMutex)
        {
            foreach (var slot in _slots)
                slot.Child.NotifyAddedToSurface(surface);
        }
    }

    public override void NotifyRemovedFromSurface(Surface surface)
    {
        base.NotifyRemovedFromSurface(surface);
        lock (SlotsMutex)
        {
            foreach (var slot in _slots)
                slot.Child.NotifyRemovedFromSurface(surface);
        }
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        lock (SlotsMutex)
        {
            foreach (var slot in _slots) slot.Child.Dispose();
        }
    }

    /// <summary>
    /// Computes the transform info of content 
    /// </summary>
    /// <param name="slot">The content slot</param>
    /// <param name="info">The Absolute Transform info of this widget</param>
    /// <param name="withPadding">Should we also account for padding ? (should be true except when used in <see cref="CollectContent"/>)</param>
    /// <returns>The Absolute Transform info of content</returns>
    public TransformInfo ComputeContentTransform(ContainerSlot slot, TransformInfo info, bool withPadding = true)
    {
        return OffsetTransformTo(slot.Child, info,withPadding);
    }

    public virtual IEnumerable<ContainerSlot> GetCollectableSlots() => GetSlots();
    public virtual IEnumerable<ContainerSlot> GetHitTestableSlots() => GetSlots();
    
    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        drawCommands.IncrDepth();
        switch (Clip)
        {
            case Clip.None:
                foreach (var slot in GetCollectableSlots())
                {
                    var newTransform = ComputeContentTransform(slot, info,false);
                    var widget = slot.Child;
                    widget.Collect(newTransform,drawCommands);
                }
                break;
            case Clip.Bounds:
            {
                drawCommands.PushClip(info,this);

                var myAAR = info.ToRect();
                
                foreach (var slot in GetCollectableSlots())
                {
                    var newTransform = ComputeContentTransform(slot, info,false);
                    var slotAAR = newTransform.ToRect();
                    
                    if(!myAAR.IntersectsWith(slotAAR)) continue;
                    
                    var widget = slot.Child;
                    widget.Collect(newTransform,drawCommands);
                }
                
                drawCommands.PopClip();
            }
                break;
        }

        drawCommands.DecrDepth();
    }
}