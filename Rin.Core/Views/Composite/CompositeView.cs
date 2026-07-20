using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Animation;
using Rin.Core.Graphics;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Layouts;
using Rin.Core.Extensions;
using Rin.Core.Shared.Math;

namespace Rin.Core.Views.Composite;

public abstract class CompositeView : View, ICompositeView
{
    public Clip Clip { get; set; } = Clip.None;

    public override void HandleEvent(ISurfaceEvent e, in Matrix4x4 absoluteTransform)
    {
        {
            if (e is IPositionalEvent asPositionalEvent)
            {
                var paddingTransform = absoluteTransform.ApplyBefore(GetPaddingOffsetTransform());

                if (Rect2D.PointWithin(GetContentSize(), paddingTransform, asPositionalEvent.Position))
                {
                    var contentTransform = absoluteTransform.ApplyBefore(GetLocalContentTransform());
                    var slots = ComputeHitTestableSlotsForEvent(asPositionalEvent, contentTransform);
                    if (slots.NotEmpty())
                    {
                        if (e is IHandleableEvent asHandleable)
                            foreach (var (slot, slotTransform) in slots)
                            {
                                slot.Child.HandleEvent(e, slotTransform);
                                if (asHandleable.Handled) return;
                            }
                        else
                            foreach (var (slot, slotTransform) in slots)
                                slot.Child.HandleEvent(e, slotTransform);
                    }
                }
            }
        }
        base.HandleEvent(e, absoluteTransform);
    }

    public override void SetSurface(ISurface? surface)
    {
        base.SetSurface(surface);
        foreach (var layoutSlot in GetSlots()) layoutSlot.Child.SetSurface(surface);
    }

    /// <summary>
    ///     Compute all the offsets applied to a child of this <see cref="CompositeView" />
    /// </summary>
    /// <param name="child"></param>
    /// <returns></returns>
    public virtual Matrix4x4 ComputeChildOffsets(IView child)
    {
        return GetLocalContentTransform().ChildOf(GetLocalTransform());
    }

    [PublicAPI]
    public abstract ISlot[] GetSlots();
    

    public virtual void OnChildLayoutInvalidated(IView child)
    {
        InvalidateDesiredSize();
        InvalidateLayout();
    }

    private readonly List<Pair<IView, Matrix4x4>> _toCollect = [];
    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        if (Visibility is Visibility.Hidden or Visibility.Collapsed) return;

        commands.IncrDepth();
        var clipRect = clip;


        var contentTransform = transform.ApplyBefore(GetLocalContentTransform());
        
        if (Parent != null && Clip == Clip.Bounds) commands.PushClip(transform.ApplyBefore(GetPaddingOffsetTransform()), GetContentSize());

        if (Clip == Clip.Bounds) clipRect = Rect2D.Clamp(ComputeAABB(contentTransform), clipRect);

        

        foreach (var slot in GetSlots())
        {
            if(!slot.Child.IsVisible) continue;
            
            var slotTransform = ComputeSlotTransform(slot, contentTransform);

            var aabb = slot.Child.ComputeAABB(slotTransform);

            if (!Rect2D.IntersectsWith(clipRect, aabb)) continue;

            _toCollect.Add(new Pair<IView, Matrix4x4>(slot.Child, slotTransform));
        }

        foreach (var (view, mat) in _toCollect) view.Collect(mat, clipRect, commands);
        
        _toCollect.Clear();

        if (Parent != null && Clip == Clip.Bounds) commands.PopClip();
    }

    [PublicAPI]
    public virtual void OnChildAdded(IView child)
    {
        child.InvalidateLayout();
    }

    [PublicAPI]
    public virtual void OnChildRemoved(IView child)
    {
        InvalidateDesiredSize();
        InvalidateLayout();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        ((IAnimatable)this).UpdateRunner();
        foreach (var slot in GetSlots()) slot.Child.Update(deltaTime);
    }

    public override void Dispose()
    {
        foreach (var slot in GetSlots()) slot.Child.Dispose();
        base.Dispose();
    }

    public virtual void OnChildNeedsLayout(IView child)
    {
        Layout(GetSize());
    }

    public virtual IView[] GetChildren()
    {
        var slots = GetSlots();
        var children = new IView[slots.Length];
        for (var i = 0; i < slots.Length; i++)
        {
            children[i] = slots[i].Child;
        }

        return children;
    }

    public override void InvalidateLayout()
    {
        base.InvalidateLayout();
        // Only cascade to currently-valid children - invalid ones are already in the pending queue.
        // When ForceLayout processes this parent first (lower depth), Layout() covers all children,
        // marking them valid; their pending entries are then skipped.
        foreach (var slot in GetSlots())
            if (slot.Child.IsLayoutValid)
                slot.Child.InvalidateLayout();
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return ArrangeContent(availableSpace);
    }


    private readonly List<Pair<ISlot, Matrix4x4>> _computeHitTestableSlotsForEventScratch = [];
    protected virtual Pair<ISlot, Matrix4x4>[] ComputeHitTestableSlotsForEvent(IPositionalEvent e, Matrix4x4 transform)
    {
        if (!IsChildrenHitTestable) return [];
        var slots = GetSlots();
        if (e.ReverseTestOrder)
        {
            for (var i = slots.Length - 1; i > -1; i--)
            {
                var slot = slots[i];
                if(!slot.Child.IsHitTestable) continue;
                var slotTransform = ComputeSlotTransform(slot, transform);
                if (slot.Child.PointWithin(slotTransform, e.Position))
                {
                    _computeHitTestableSlotsForEventScratch.Add(new(slot, slotTransform));
                }
            }
        }
        else
        {
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if(!slot.Child.IsHitTestable) continue;
                var slotTransform = ComputeSlotTransform(slot, transform);
                if (slot.Child.PointWithin(slotTransform, e.Position))
                {
                    _computeHitTestableSlotsForEventScratch.Add(new(slot, slotTransform));
                }
            }
        }

        var result = _computeHitTestableSlotsForEventScratch.ToArray();
        _computeHitTestableSlotsForEventScratch.Clear();
        return result;
    }

    /// <summary>
    ///     Compute extra offsets for this slot
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="contentTransform"></param>
    /// <returns></returns>
    protected virtual Matrix4x4 ComputeSlotTransform(ISlot slot, in Matrix4x4 contentTransform)
    {
        return slot.Child.GetLocalTransform().ChildOf(contentTransform);
    }

    /// <summary>
    ///     Arranges content and returns their computed total size i.e. the combined length of all items in a list
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <returns></returns>
    protected abstract Vector2 ArrangeContent(in Vector2 availableSpace);
}