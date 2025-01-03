﻿using JetBrains.Annotations;
using rin.Framework.Core;
using rin.Framework.Core.Animation;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Views.Enums;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Layouts;

namespace rin.Framework.Views.Composite;

public abstract class CompositeView : View
{
   public Clip Clip = Clip.None;
    
    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        return ArrangeContent(availableSpace);
    }
    public override bool NotifyCursorDown(CursorDownEvent e, Mat3 transform)
    {
        if (IsChildrenHitTestable)
        {
            var res = ChildrenNotifyCursorDown(e, transform.Translate(new Vec2<float>(Padding.Left, Padding.Top)));
            if (res) return res;
        }

        return base.NotifyCursorDown(e, transform);
    }
    public override void NotifyCursorEnter(CursorMoveEvent e, Mat3 transform, List<View> items)
    {
        if (IsChildrenHitTestable)
            ChildrenNotifyCursorEnter(e, transform.Translate(new Vec2<float>(Padding.Left, Padding.Top)), items);

        base.NotifyCursorEnter(e, transform, items);
    }
    public override bool NotifyCursorMove(CursorMoveEvent e, Mat3 transform)
    {
        if (IsChildrenHitTestable &&
            ChildrenNotifyCursorMove(e, transform.Translate(new Vec2<float>(Padding.Left, Padding.Top))))
            return true;

        return base.NotifyCursorMove(e, transform);
    }
    public override bool NotifyScroll(ScrollEvent e, Mat3 transform)
    {
        if (IsChildrenHitTestable &&
            ChildrenNotifyScroll(e, transform.Translate(new Vec2<float>(Padding.Left, Padding.Top)))) return true;

        return base.NotifyScroll(e, transform);
    }
    protected virtual bool ChildrenNotifyCursorDown(CursorDownEvent e, Mat3 transform)
    {
        foreach (var slot in GetHitTestableSlots().AsReversed())
        {
            var slotTransform = ComputeSlotTransform(slot, transform);
            if (!slot.Child.PointWithin(slotTransform, e.Position)) continue;
            var res = slot.Child.NotifyCursorDown(e, slotTransform);
            if (res) return true;
        }

        return false;
    }
    protected virtual void ChildrenNotifyCursorEnter(CursorMoveEvent e, Mat3 transform, List<View> items)
    {
        foreach (var slot in GetHitTestableSlots())
        {
            var slotTransform = ComputeSlotTransform(slot, transform);
            if (slot.Child.PointWithin(slotTransform, e.Position))
                slot.Child.NotifyCursorEnter(e, slotTransform, items);
        }
    }
    protected virtual bool ChildrenNotifyCursorMove(CursorMoveEvent e, Mat3 transform)
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

    protected virtual bool ChildrenNotifyScroll(ScrollEvent e, Mat3 transform)
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

    public override void SetSurface(Surface? surface)
    {
        base.SetSurface(surface);
        foreach (var layoutSlot in GetSlots())
        {
            layoutSlot.Child.SetSurface(surface);
        }
    }

    /// <summary>
    /// Compute extra offsets for this slot
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="contentTransform"></param>
    /// <returns></returns>
    protected virtual Mat3 ComputeSlotTransform(ISlot slot, Mat3 contentTransform)
    {
        var relativeTransform = slot.Child.ComputeRelativeTransform();
        return contentTransform * relativeTransform;
    }

    [PublicAPI]
    public abstract IEnumerable<ISlot> GetSlots();
    [PublicAPI]
    public virtual IEnumerable<ISlot> GetCollectableSlots() => GetSlots().Where(c => c.Child.Visibility is not Visibility.Collapsed);
    [PublicAPI]
    public virtual IEnumerable<ISlot> GetHitTestableSlots() => GetSlots().Where(c => c.Child.IsHitTestable);
    
    public override void Collect(Mat3 transform, Views.Rect clip, DrawCommands drawCommands)
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

        var transformWithPadding = transform.Translate(new Vec2<float>(Padding.Left, Padding.Top));

        List<Pair<View, Mat3>> toCollect = [];
        
        foreach (var slot in GetCollectableSlots())
        {
            var slotTransform = ComputeSlotTransform(slot, transformWithPadding);

            var aabb = slot.Child.ComputeAABB(slotTransform);

            if (!clipRect.IntersectsWith(aabb)) continue;
            
            toCollect.Add(new Pair<View, Mat3>(slot.Child,slotTransform));
        }
        
        foreach (var (view, mat) in toCollect)
        {
            view.Collect(mat,clipRect,drawCommands);
        }

        if (Parent != null && Clip == Clip.Bounds)
        {
            drawCommands.PopClip();
        }
    }
    
    /// <summary>
    /// Arranges content and returns their computed total size i.e. the combined length of all items in a list
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <returns></returns>
    protected abstract Vec2<float> ArrangeContent(Vec2<float> availableSpace);
    
    [PublicAPI]
    public abstract void OnChildInvalidated(View child, InvalidationType invalidation);

    [PublicAPI]
    public virtual void OnChildAdded(View child)
    {
        Invalidate(InvalidationType.Layout);
    }

    [PublicAPI]
    public virtual void OnChildRemoved(View child)
    {
        Invalidate(InvalidationType.Layout);
    }
}