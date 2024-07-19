using aerox.Runtime.Extensions;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Widgets.Graphics.Commands;

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
    protected readonly List<T> Slots = new();
    protected readonly Mutex SlotsMutex = new();

    public Container(params Widget[] children)
    {
        foreach (var widget in children)
            if (AddChildFromConstructor(widget) == null)
                break;
    }

    protected T? AddChildFromConstructor(Widget widget) => AddChild(widget);

    public virtual T? AddChild<TE>() where TE : Widget
    {
        return AddChild(Activator.CreateInstance<TE>());
    }

    public virtual T? AddChild(Widget widget)
    {
        lock (SlotsMutex)
        {
            var maxSlots = GetMaxSlots();
            if (maxSlots > 0 && Slots.Count == maxSlots) return null;

            widget.SetParent(this);

            var slot = MakeSlot(widget);

            Slots.Add(slot);

            if (Surface != null) widget.NotifyAddedToSurface(Surface);

            OnChildResized(widget);
            Console.WriteLine("Added child [{0}] to container [{1}]", widget.GetType().Name, GetType().Name);
            return slot;
        }
    }

    public virtual bool RemoveChild(Widget widget)
    {
        lock (SlotsMutex)
        {
            for (var i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].GetWidget() != widget) continue;

                widget.SetParent(null);

                if (Surface != null) widget.NotifyRemovedFromSurface(Surface);

                Slots.RemoveAt(i);

                CheckSize();
                return true;
            }

            return false;
        }
    }

    public virtual T? GetChildSlot(int idx)
    {
        lock (SlotsMutex)
        {
            return idx < Slots.Count ? Slots[idx] : null;
        }
    }

    public virtual T[] GetSlots()
    {
        lock (SlotsMutex)
        {
            return Slots.ToArray();
        }
    }

    public virtual int GetNumSlots()
    {
        lock (SlotsMutex)
        {
            return Slots.Count;
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
        lock (SlotsMutex)
        {
            foreach (var slot in Slots.AsReversed())
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
        lock (SlotsMutex)
        {
            foreach (var slot in Slots)
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
        lock (SlotsMutex)
        {
            foreach (var slot in Slots)
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
        lock (SlotsMutex)
        {
            foreach (var slot in Slots)
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
        ArrangeSlots(GetDrawSize());
    }

    public override void NotifyAddedToSurface(WidgetSurface widgetSurface)
    {
        base.NotifyAddedToSurface(widgetSurface);
        lock (SlotsMutex)
        {
            foreach (var slot in Slots)
                slot.GetWidget().NotifyAddedToSurface(widgetSurface);
        }
    }

    public override void NotifyRemovedFromSurface(WidgetSurface widgetSurface)
    {
        base.NotifyRemovedFromSurface(widgetSurface);
        lock (SlotsMutex)
        {
            foreach (var slot in Slots)
                slot.GetWidget().NotifyRemovedFromSurface(widgetSurface);
        }
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        lock (SlotsMutex)
        {
            foreach (var slot in Slots) slot.GetWidget().Dispose();
        }
    }

    // public override void Collect(WidgetFrame frame, DrawInfo info)
    // {
    //     frame.AddCommands(new SimpleClip(info.ClipRect));
    // }
    //
    // protected virtual void CollectChildren(WidgetFrame frame, DrawInfo info)
    // {
    //     
    // }
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