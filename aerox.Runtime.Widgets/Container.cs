using aerox.Runtime.Widgets.Events;

namespace aerox.Runtime.Widgets;

public abstract class ContainerBase : Widget
{
    protected abstract void ArrangeSlots(Size2d drawSize);

    public virtual void OnChildResized(Widget widget)
    {
        if (CheckSize()) ArrangeSlots(GetDrawSize());
    }
}

public abstract class Container<T> : ContainerBase where T : Slot
{
    protected readonly List<T> slots = new();
    protected readonly Mutex slotsMutex = new();

    public Container(params Widget[] children)
    {
        foreach (var widget in children)
            if (AddChild(widget) == null)
                break;
    }

    public virtual T? AddChild<TE>() where TE : Widget
    {
        return AddChild(Activator.CreateInstance<TE>());
    }

    public virtual T? AddChild(Widget widget)
    {
        lock (slotsMutex)
        {
            var maxSlots = GetMaxSlots();
            if (maxSlots > 0 && slots.Count == maxSlots) return null;

            widget.SetParent(this);

            var slot = MakeSlot(widget);

            slots.Add(slot);

            if (Surface != null) widget.NotifyAddedToRoot(Surface);

            OnChildResized(widget);
            Console.WriteLine("Added child [{0}] to container [{1}]", widget.GetType().Name, GetType().Name);
            return slot;
        }
    }

    public virtual bool RemoveChild(Widget widget)
    {
        lock (slotsMutex)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].GetWidget() != widget) continue;

                widget.SetParent(null);

                if (Surface != null) widget.NotifyRemovedFromRoot(Surface);

                slots.RemoveAt(i);

                CheckSize();
                return true;
            }

            return false;
        }
    }

    public virtual T? GetChildSot(int idx)
    {
        lock (slotsMutex)
        {
            return idx < slots.Count ? slots[idx] : null;
        }
    }

    public virtual T[] GetSlots()
    {
        lock (slotsMutex)
        {
            return slots.ToArray();
        }
    }

    public virtual int GetNumSlots()
    {
        lock (slotsMutex)
        {
            return slots.Count;
        }
    }

    public abstract uint GetMaxSlots();
    public abstract T MakeSlot(Widget widget);

    public override Widget? ReceiveCursorDown(CursorDownEvent e, DrawInfo info)
    {
        if (IsChildrenHitTestable())
        {
            var res = ChildrenReceiveCursorDown(e, info);
            if (res != null ) return res;
        }

        return base.ReceiveCursorDown(e, info);
    }

    protected virtual Widget? ChildrenReceiveCursorDown(CursorDownEvent e, DrawInfo info)
    {
        var point = e.Position.Cast<float>();
        lock (slotsMutex)
        {
            foreach (var slot in slots)
            {
                var slotInfo = info.AccountFor(slot.GetWidget());
                if (!slotInfo.PointWithin(point)) continue;
                var res = slot.GetWidget().ReceiveCursorDown(e, slotInfo);
                if (res != null) return res;
            }
        }

        return null;
    }

    public override void ReceiveCursorEnter(CursorMoveEvent e, DrawInfo info, List<Widget> items)
    {
        if (IsChildrenHitTestable()) ChildrenReceiveCursorEnter(e, info, items);

        base.ReceiveCursorEnter(e, info, items);
    }

    protected virtual void ChildrenReceiveCursorEnter(CursorMoveEvent e, DrawInfo info, List<Widget> items)
    {
        var point = e.Position.Cast<float>();
        lock (slotsMutex)
        {
            foreach (var slot in slots)
            {
                var slotInfo = info.AccountFor(slot.GetWidget());
                if (slotInfo.PointWithin(point))
                    slot.GetWidget().ReceiveCursorEnter(e, slotInfo, items);
            }
        }
    }

    public override bool ReceiveCursorMove(CursorMoveEvent e, DrawInfo info)
    {
        if (IsChildrenHitTestable() && ChildrenReceiveCursorMove(e, info)) return true;

        return base.ReceiveCursorMove(e, info);
    }

    protected virtual bool ChildrenReceiveCursorMove(CursorMoveEvent e, DrawInfo info)
    {
        var point = e.Position.Cast<float>();
        lock (slotsMutex)
        {
            foreach (var slot in slots)
            {
                var slotInfo = info.AccountFor(slot.GetWidget());
                if (slotInfo.PointWithin(point) &&
                    slot.GetWidget().ReceiveCursorMove(e, slotInfo))
                    return true;
            }
        }

        return false;
    }

    public override bool ReceiveScroll(ScrollEvent e, DrawInfo info)
    {
        if (IsChildrenHitTestable() && ChildrenReceiveScroll(e, info)) return true;

        return base.ReceiveScroll(e, info);
    }

    protected virtual bool ChildrenReceiveScroll(ScrollEvent e, DrawInfo info)
    {
        var point = e.Position.Cast<float>();
        lock (slotsMutex)
        {
            foreach (var slot in slots)
            {
                var slotInfo = info.AccountFor(slot.GetWidget());
                if (slotInfo.PointWithin(point) &&
                    slot.GetWidget().ReceiveScroll(e, slotInfo))
                    return true;
            }
        }

        return false;
    }

    public override void SetDrawSize(Size2d size)
    {
        base.SetDrawSize(size);
        ArrangeSlots(size);
    }

    public override void NotifyAddedToRoot(WidgetSurface widgetSurface)
    {
        base.NotifyAddedToRoot(widgetSurface);
        lock (slotsMutex)
        {
            foreach (var slot in slots)
                slot.GetWidget().NotifyAddedToRoot(widgetSurface);
        }
    }

    public override void NotifyRemovedFromRoot(WidgetSurface widgetSurface)
    {
        base.NotifyRemovedFromRoot(widgetSurface);
        lock (slotsMutex)
        {
            foreach (var slot in slots)
                slot.GetWidget().NotifyRemovedFromRoot(widgetSurface);
        }
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        lock (slotsMutex)
        {
            foreach (var slot in slots) slot.GetWidget().Dispose();
        }
    }
}

public abstract class Container : Container<Slot>
{
    public Container(params Widget[] children) : base(children)
    {
    }

    public override Slot MakeSlot(Widget widget)
    {
        return new Slot(widget);
    }
}