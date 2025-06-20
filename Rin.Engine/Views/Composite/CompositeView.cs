using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Animation;
using Rin.Engine.Extensions;
using Rin.Engine.Math;
using Rin.Engine.Views.Enums;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Layouts;

namespace Rin.Engine.Views.Composite;

public abstract class CompositeView : View
{
    public Clip Clip = Clip.None;

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return ArrangeContent(availableSpace);
    }

    public override void HandleEvent(ISurfaceEvent e, in Matrix4x4 transform)
    {
        var withPadding = transform.Translate(new Vector2(Padding.Left, Padding.Top));
        var slots = ComputeHitTestableSlotsForEvent(e, withPadding);
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

        base.HandleEvent(e, transform);
    }

    protected virtual Pair<ISlot, Matrix4x4>[] ComputeHitTestableSlotsForEvent(ISurfaceEvent e, Matrix4x4 transform)
    {
        switch (e)
        {
            case CursorDownSurfaceEvent ev:
                if (IsChildrenHitTestable)
                    return GetHitTestableSlots()
                        .Select(c => new Pair<ISlot, Matrix4x4>(c, ComputeSlotTransform(c, transform)))
                        .Where(c => c.First.Child.PointWithin(c.Second, ev.Position))
                        .AsReversed()
                        .ToArray();
                break;
            case CursorMoveSurfaceEvent ev:
                if (IsChildrenHitTestable)
                    return GetHitTestableSlots()
                        .Select(c => new Pair<ISlot, Matrix4x4>(c, ComputeSlotTransform(c, transform)))
                        .Where(c => c.First.Child.PointWithin(c.Second, ev.Position))
                        .ToArray();
                break;
            case ScrollSurfaceEvent ev:
                if (IsChildrenHitTestable)
                    return GetHitTestableSlots()
                        .Select(c => new Pair<ISlot, Matrix4x4>(c, ComputeSlotTransform(c, transform)))
                        .Where(c => c.First.Child.PointWithin(c.Second, ev.Position))
                        .ToArray();
                break;
        }

        return [];
    }

    public override void SetSurface(Surface? surface)
    {
        base.SetSurface(surface);
        foreach (var layoutSlot in GetSlots()) layoutSlot.Child.SetSurface(surface);
    }

    /// <summary>
    ///     Compute extra offsets for this slot
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="contentTransform"></param>
    /// <returns></returns>
    protected virtual Matrix4x4 ComputeSlotTransform(ISlot slot, in Matrix4x4 contentTransform)
    {
        var relativeTransform = slot.Child.GetLocalTransform();
        return relativeTransform * contentTransform;
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

    public override void Collect(in Matrix4x4 transform, in Views.Rect clip, CommandList commands)
    {
        if (Visibility is Visibility.Hidden or Visibility.Collapsed) return;

        commands.IncrDepth();
        var clipRect = clip;

        if (Parent != null && Clip == Clip.Bounds) commands.PushClip(transform, GetContentSize());

        if (Clip == Clip.Bounds) clipRect = ComputeAABB(transform).Clamp(clipRect);

        var transformWithPadding = transform.Translate(new Vector2(Padding.Left, Padding.Top));

        List<Pair<View, Matrix4x4>> toCollect = [];

        foreach (var slot in GetCollectableSlots())
        {
            var slotTransform = ComputeSlotTransform(slot, transformWithPadding);

            var aabb = slot.Child.ComputeAABB(slotTransform);

            if (!clipRect.IntersectsWith(aabb)) continue;

            toCollect.Add(new Pair<View, Matrix4x4>(slot.Child, slotTransform));
        }

        foreach (var (view, mat) in toCollect) view.Collect(mat, clipRect, commands);

        if (Parent != null && Clip == Clip.Bounds) commands.PopClip();
    }

    /// <summary>
    ///     Arranges content and returns their computed total size i.e. the combined length of all items in a list
    /// </summary>
    /// <param name="availableSpace"></param>
    /// <returns></returns>
    protected abstract Vector2 ArrangeContent(in Vector2 availableSpace);

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
}