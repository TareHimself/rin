using aerox.Runtime.Extensions;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Widgets.Graphics.Commands;

namespace aerox.Runtime.Widgets;
public abstract class Container : Widget
{
    private readonly List<Slot> _slot = new();
    
    protected readonly Mutex SlotsMutex = new();
    
    public ClipMode Clip = ClipMode.None;
    
    public Container(IEnumerable<Widget> children)
    {
        foreach (var widget in children)
            if (AddChild(widget) == null)
                break;
    }
    
    public Container(IEnumerable<Slot> children)
    {
        foreach (var slot in children)
            if (AddChild(slot) == null)
                break;
    }
    
    public Container()
    {
    }
    
    protected abstract void ArrangeSlots(Size2d drawSize);

    public virtual void OnChildResized(Widget widget)
    {
        if (CheckSize()) ArrangeSlots(GetContentSize());
    }
    
    public virtual Slot MakeSlot(Widget widget)
    {
        return new Slot(widget,this);
    }
    
    public virtual Slot? AddChild<TE>() where TE : Widget
    {
        return AddChild(Activator.CreateInstance<TE>());
    }
    
    public virtual void OnSlotUpdated(Slot slot)
    {
    }

    public Slot? AddChild(Widget widget)
    {
        lock (SlotsMutex)
        {
            var maxSlots = GetMaxSlots();
            if (maxSlots > 0 && _slot.Count == maxSlots) return null;

            widget.SetParent(this);

            var slot = MakeSlot(widget);
            
            slot.SetOwner(this);
            
            _slot.Add(slot);

            if (Surface != null) widget.NotifyAddedToSurface(Surface);

            OnChildResized(widget);
            Console.WriteLine("Added child [{0}] to container [{1}]", widget.GetType().Name, GetType().Name);
            return slot;
        }
    }
    
    public Slot? AddChild(Slot slot)
    {
        lock (SlotsMutex)
        {
            var maxSlots = GetMaxSlots();
            if (maxSlots > 0 && _slot.Count == maxSlots) return null;
                
            var widget = slot.GetWidget();
            
            widget.SetParent(this);
            
            slot.SetOwner(this);
            
            _slot.Add(slot);

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
            for (var i = 0; i < _slot.Count; i++)
            {
                if (_slot[i].GetWidget() != widget) continue;

                widget.SetParent(null);

                if (Surface != null) widget.NotifyRemovedFromSurface(Surface);

                _slot.RemoveAt(i);

                CheckSize();
                return true;
            }

            return false;
        }
    }

    public virtual Slot? GetSlot(int idx)
    {
        lock (SlotsMutex)
        {
            return idx < _slot.Count ? _slot[idx] : null;
        }
    }

    public virtual Slot[] GetSlots()
    {
        lock (SlotsMutex)
        {
            return _slot.ToArray();
        }
    }

    public virtual int GetNumSlots()
    {
        lock (SlotsMutex)
        {
            return _slot.Count;
        }
    }

    public virtual uint GetMaxSlots() => 0;

    public override Widget? ReceiveCursorDown(CursorDownEvent e, TransformInfo info)
    {
        if (IsChildrenHitTestable())
        {
            var res = ChildrenReceiveCursorDown(e, info);
            if (res != null ) return res;
        }

        return base.ReceiveCursorDown(e, info);
    }

    protected virtual Widget? ChildrenReceiveCursorDown(CursorDownEvent e, TransformInfo info)
    {
        var point = e.Position.Cast<float>();
        lock (SlotsMutex)
        {
            foreach (var slot in _slot.AsReversed())
            {
                var slotInfo = ComputeChildTransform(slot, info);
                if (!slotInfo.PointWithin(point)) continue;
                var res = slot.GetWidget().ReceiveCursorDown(e, slotInfo);
                if (res != null) return res;
            }
        }

        return null;
    }

    public override void ReceiveCursorEnter(CursorMoveEvent e, TransformInfo info, List<Widget> items)
    {
        if (IsChildrenHitTestable()) ChildrenReceiveCursorEnter(e, info, items);

        base.ReceiveCursorEnter(e, info, items);
    }

    protected virtual void ChildrenReceiveCursorEnter(CursorMoveEvent e, TransformInfo info, List<Widget> items)
    {
        var point = e.Position.Cast<float>();
        lock (SlotsMutex)
        {
            foreach (var slot in _slot)
            {
                var slotInfo = ComputeChildTransform(slot, info);
                if (slotInfo.PointWithin(point))
                    slot.GetWidget().ReceiveCursorEnter(e, slotInfo, items);
            }
        }
    }

    public override bool ReceiveCursorMove(CursorMoveEvent e, TransformInfo info)
    {
        if (IsChildrenHitTestable() && ChildrenReceiveCursorMove(e, info)) return true;

        return base.ReceiveCursorMove(e, info);
    }

    protected virtual bool ChildrenReceiveCursorMove(CursorMoveEvent e, TransformInfo info)
    {
        var point = e.Position.Cast<float>();
        lock (SlotsMutex)
        {
            foreach (var slot in _slot)
            {
                var slotInfo = ComputeChildTransform(slot, info);
                if (slotInfo.PointWithin(point) &&
                    slot.GetWidget().ReceiveCursorMove(e, slotInfo))
                    return true;
            }
        }

        return false;
    }

    public override bool ReceiveScroll(ScrollEvent e, TransformInfo info)
    {
        if (IsChildrenHitTestable() && ChildrenReceiveScroll(e, info)) return true;

        return base.ReceiveScroll(e, info);
    }

    protected virtual bool ChildrenReceiveScroll(ScrollEvent e, TransformInfo info)
    {
        var point = e.Position.Cast<float>();
        lock (SlotsMutex)
        {
            foreach (var slot in _slot)
            {
                var slotInfo = ComputeChildTransform(slot, info);
                if (slotInfo.PointWithin(point) &&
                    slot.GetWidget().ReceiveScroll(e, slotInfo))
                    return true;
            }
        }

        return false;
    }

    public override void SetSize(Size2d size)
    {
        base.SetSize(size);
        ArrangeSlots(GetContentSize());
    }

    public override void NotifyAddedToSurface(Surface surface)
    {
        base.NotifyAddedToSurface(surface);
        lock (SlotsMutex)
        {
            foreach (var slot in _slot)
                slot.GetWidget().NotifyAddedToSurface(surface);
        }
    }

    public override void NotifyRemovedFromSurface(Surface surface)
    {
        base.NotifyRemovedFromSurface(surface);
        lock (SlotsMutex)
        {
            foreach (var slot in _slot)
                slot.GetWidget().NotifyRemovedFromSurface(surface);
        }
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        lock (SlotsMutex)
        {
            foreach (var slot in _slot) slot.GetWidget().Dispose();
        }
    }

    public TransformInfo ComputeChildTransform(Slot slot, TransformInfo info)
    {
        return ComputeChildTransform(slot.GetWidget(), info);
    }

    public virtual TransformInfo ComputeChildTransform(Widget widget, TransformInfo info)
    {
        return new TransformInfo(info.Transform.Translate(new Vector2<float>(Padding.Left,Padding.Top)) * widget.ComputeRelativeTransform(),widget.GetSize(),info.Depth + 1);
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        switch (Clip)
        {
            case ClipMode.None:
                foreach (var slot in GetSlots())
                {
                    var newTransform = ComputeChildTransform(slot, info);
                    var widget = slot.GetWidget();
                    widget.Collect(newTransform,drawCommands);
                }
                break;
            case ClipMode.Bounds:
            {
                drawCommands.PushClip(info,this);

                var myAAR = info.ToRect();
                
                foreach (var slot in GetSlots())
                {
                    var newTransform = ComputeChildTransform(slot, info);
                    var slotAAR = newTransform.ToRect();
                    
                    if(!myAAR.IntersectsWith(slotAAR)) continue;
                    
                    var widget = slot.GetWidget();
                    widget.Collect(newTransform,drawCommands);
                }
                
                drawCommands.PopClip();
            }
                break;
        }
    }
}