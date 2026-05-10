using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Animation;
using Rin.Framework.Extensions;
using Rin.Framework.Graphics;
using Rin.Framework.Shared.Math;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

public abstract class CompositeView : View, ICompositeView
{
    public Clip Clip { get; set; } = Clip.None;

    public override void HandleEvent(ISurfaceEvent e, in Matrix4x4 absoluteTransform)
    {
        {
            if (e is IPositionalEvent asPositionalEvent)
            {
                var contentTransform = absoluteTransform.ApplyBefore(GetLocalContentTransform());

                if (Rect2D.PointWithin(GetContentSize(), contentTransform, asPositionalEvent.Position))
                {
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
    public abstract IEnumerable<ISlot> GetSlots();

    [PublicAPI]
    public virtual IEnumerable<ISlot> GetCollectableSlots()
    {
        return GetSlots().Where(c => c.Child.Visibility is not Visibility.Collapsed);
    }

    [PublicAPI]
    public virtual IEnumerable<ISlot> GetHitTestableSlots()
    {
        return GetSlots().Where(c => c.Child.IsHitTestable);
    }

    public virtual void OnChildLayoutInvalidated(IView child)
    {
        InvalidateDesiredSize();
        InvalidateLayout();
    }

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        if (Visibility is Visibility.Hidden or Visibility.Collapsed) return;

        commands.IncrDepth();
        var clipRect = clip;


        var contentTransform = transform.ApplyBefore(GetLocalContentTransform());

        if (Parent != null && Clip == Clip.Bounds) commands.PushClip(contentTransform, GetContentSize());

        if (Clip == Clip.Bounds) clipRect = ComputeAABB(contentTransform).Clamp(clipRect);

        List<Pair<IView, Matrix4x4>> toCollect = [];

        foreach (var slot in GetCollectableSlots())
        {
            var slotTransform = ComputeSlotTransform(slot, contentTransform);

            var aabb = slot.Child.ComputeAABB(slotTransform);

            if (!clipRect.IntersectsWith(aabb)) continue;

            toCollect.Add(new Pair<IView, Matrix4x4>(slot.Child, slotTransform));
        }

        foreach (var (view, mat) in toCollect) view.Collect(mat, clipRect, commands);

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

    public virtual void LayoutChild(IView child)
    {
        Layout(GetSize());
    }

    public override void InvalidateLayout()
    {
        base.InvalidateLayout();
        foreach (var slot in GetSlots())
            if (slot.Child.IsLayoutValid)
                slot.Child.InvalidateLayout();
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return ArrangeContent(availableSpace);
    }

    protected virtual Pair<ISlot, Matrix4x4>[] ComputeHitTestableSlotsForEvent(IPositionalEvent e, Matrix4x4 transform)
    {
        if (!IsChildrenHitTestable) return [];
        var enumerator = GetHitTestableSlots()
            .Select(c => new Pair<ISlot, Matrix4x4>(c, ComputeSlotTransform(c, transform)))
            .Where(c => c.First.Child.PointWithin(c.Second, e.Position));
        if (e.ReverseTestOrder) enumerator = enumerator.AsReversed();

        return enumerator.ToArray();
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