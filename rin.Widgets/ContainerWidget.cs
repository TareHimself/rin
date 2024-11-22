using System.Collections.Concurrent;
using rin.Core.Animation;
using rin.Core.Extensions;
using rin.Core.Math;
using rin.Widgets.Enums;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Commands;

namespace rin.Widgets;

public abstract class ContainerWidget : Widget
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

    /// <summary>
    /// Arranges content and returns their computed total size i.e. the combined length of all items in a list
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <returns></returns>
    protected abstract Vector2<float> ArrangeContent(Vector2<float> availableSpace);


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

    public virtual void OnChildInvalidated(Widget child, InvalidationType invalidation)
    {
        if (_widgetSlotMap.TryGetValue(child, out var slot))
        {
            OnSlotInvalidated(slot, invalidation);
        }
    }

    public virtual void OnSlotInvalidated(ContainerSlot slot, InvalidationType invalidation)
    {
        Invalidate(invalidation);
    }

    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        return ArrangeContent(availableSpace);
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
            _widgetSlotMap.TryAdd(widget, slot);

            if (Surface != null)
            {
                widget.NotifyAddedToSurface(Surface);
                OnSlotInvalidated(slot, InvalidationType.DesiredSize);
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
                _widgetSlotMap.TryRemove(widget, out var _);

                if (Surface != null)
                {
                    Invalidate(InvalidationType.DesiredSize);
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

    public override Widget? NotifyCursorDown(CursorDownEvent e, Matrix3 transform)
    {
        if (IsChildrenHitTestable)
        {
            var res = ChildrenNotifyCursorDown(e, transform.Translate(new Vector2<float>(Padding.Left, Padding.Top)));
            if (res != null) return res;
        }

        return base.NotifyCursorDown(e, transform);
    }

    protected virtual Widget? ChildrenNotifyCursorDown(CursorDownEvent e, Matrix3 transform)
    {
        foreach (var slot in GetHitTestableSlots().AsReversed())
        {
            var slotTransform = ComputeSlotTransform(slot, transform);
            if (!slot.Child.PointWithin(slotTransform, e.Position)) continue;
            var res = slot.Child.NotifyCursorDown(e, slotTransform);
            if (res != null) return res;
        }

        return null;
    }

    public override void NotifyCursorEnter(CursorMoveEvent e, Matrix3 transform, List<Widget> items)
    {
        if (IsChildrenHitTestable)
            ChildrenNotifyCursorEnter(e, transform.Translate(new Vector2<float>(Padding.Left, Padding.Top)), items);

        base.NotifyCursorEnter(e, transform, items);
    }

    protected virtual void ChildrenNotifyCursorEnter(CursorMoveEvent e, Matrix3 transform, List<Widget> items)
    {
        foreach (var slot in GetHitTestableSlots())
        {
            var slotTransform = ComputeSlotTransform(slot, transform);
            if (slot.Child.PointWithin(slotTransform, e.Position))
                slot.Child.NotifyCursorEnter(e, slotTransform, items);
        }
    }

    public override bool NotifyCursorMove(CursorMoveEvent e, Matrix3 transform)
    {
        if (IsChildrenHitTestable &&
            ChildrenNotifyCursorMove(e, transform.Translate(new Vector2<float>(Padding.Left, Padding.Top))))
            return true;

        return base.NotifyCursorMove(e, transform);
    }

    protected virtual bool ChildrenNotifyCursorMove(CursorMoveEvent e, Matrix3 transform)
    {
        foreach (var slot in GetHitTestableSlots())
        {
            var slotTransform = ComputeSlotTransform(slot, transform);
            if (slot.Child.PointWithin(slotTransform, e.Position) &&
                slot.Child.NotifyCursorMove(e, slotTransform))
                return true;
        }

        return false;
    }

    public override bool NotifyScroll(ScrollEvent e, Matrix3 transform)
    {
        if (IsChildrenHitTestable &&
            ChildrenNotifyScroll(e, transform.Translate(new Vector2<float>(Padding.Left, Padding.Top)))) return true;

        return base.NotifyScroll(e, transform);
    }

    protected virtual bool ChildrenNotifyScroll(ScrollEvent e, Matrix3 transform)
    {
        foreach (var slot in GetHitTestableSlots())
        {
            var slotTransform = ComputeSlotTransform(slot, transform);
            if (slot.Child.PointWithin(slotTransform, e.Position) &&
                slot.Child.NotifyScroll(e, slotTransform))
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
    /// Compute extra offsets for this slot
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="contentTransform"></param>
    /// <returns></returns>
    protected virtual Matrix3 ComputeSlotTransform(ContainerSlot slot, Matrix3 contentTransform)
    {
        var relativeTransform = slot.Child.ComputeRelativeTransform();
        return contentTransform * relativeTransform;
    }

    public virtual IEnumerable<ContainerSlot> GetCollectableSlots() => GetSlots();
    public virtual IEnumerable<ContainerSlot> GetHitTestableSlots() => GetSlots();

    protected virtual void CollectSlots(Matrix3 transform, Rect clip, DrawCommands drawCommands)
    {
    }

    public override void Collect(Matrix3 transform, Rect clip, DrawCommands drawCommands)
    {
        ((IAnimatable)this).Update();
        
        if (Visibility is Visibility.Hidden or Visibility.Collapsed)
        {
            return;
        }

        drawCommands.IncrDepth();
        var clipRect = clip;

        if (Parent != null && Clip == Clip.Bounds)
        {
            drawCommands.PushClip(transform, GetContentSize());
        }

        if (Clip == Clip.Bounds)
        {
            clipRect = ComputeAABB(transform).Clamp(clipRect);
        }

        var transformWithPadding = transform.Translate(new Vector2<float>(Padding.Left, Padding.Top));

        foreach (var slot in GetCollectableSlots())
        {
            var slotTransform = ComputeSlotTransform(slot, transformWithPadding);

            var aabb = slot.Child.ComputeAABB(slotTransform);

            if (!clipRect.IntersectsWith(aabb)) continue;

            slot.Child.Collect(slotTransform, clipRect, drawCommands);
        }

        if (Parent != null && Clip == Clip.Bounds)
        {
            drawCommands.PopClip();
        }
    }
}