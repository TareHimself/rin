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

    public override void HandleEvent(ISurfaceEvent e, in Matrix4x4 boxToSurface)
    {
        {
            if (e is IPositionalEvent asPositionalEvent)
            {
                var viewportToSurface = ViewportToBox() * boxToSurface;

                if (Rect2D.PointWithin(GetContentSize(), viewportToSurface, asPositionalEvent.Position))
                {
                    var contentToSurface = ContentToBox() * boxToSurface;
                    var slots = ComputeHitTestableSlotsForEvent(asPositionalEvent, contentToSurface);
                    if (slots.NotEmpty())
                    {
                        if (e is IHandleableEvent asHandleable)
                            foreach (var (slot, childBoxToSurface) in slots)
                            {
                                slot.Child.HandleEvent(e, childBoxToSurface);
                                if (asHandleable.Handled) return;
                            }
                        else
                            foreach (var (slot, childBoxToSurface) in slots)
                                slot.Child.HandleEvent(e, childBoxToSurface);
                    }
                }
            }
        }
        base.HandleEvent(e, boxToSurface);
    }

    public override void SetSurface(ISurface? surface)
    {
        base.SetSurface(surface);
        foreach (var layoutSlot in GetSlots()) layoutSlot.Child.SetSurface(surface);
    }

    [PublicAPI]
    public abstract ISlot[] GetSlots();

    /// <summary>
    ///     The slots participating in the current frame (layout, draw, hit-test). Defaults to
    ///     <see cref="GetSlots" />; lifetime concerns (surface, dispose) always use that.
    /// </summary>
    [PublicAPI]
    public virtual ISlot[] GetActiveSlots()
    {
        return GetSlots();
    }


    public virtual void OnChildLayoutInvalidated(IView child)
    {
        InvalidateDesiredSize();
        InvalidateLayout();
    }

    private readonly List<(IView View, Matrix4x4 Transform)> _toCollect = [];
    public override void Collect(in Matrix4x4 boxToSurface, in Rect2D clip, CommandList commands)
    {
        if (Visibility is Visibility.Hidden or Visibility.Collapsed) return;

        commands.IncrDepth();
        var clipRect = clip;

        var contentToSurface = ContentToBox() * boxToSurface;

        var viewportToSurface = ViewportToBox() * boxToSurface;
        var viewportSize = GetContentSize();

        if (Parent != null && Clip == Clip.Bounds) commands.PushClip(viewportToSurface, viewportSize);

        if (Clip == Clip.Bounds) clipRect = Rect2D.Clamp(ComputeAABB(viewportSize, viewportToSurface), clipRect);


        foreach (var slot in GetActiveSlots())
        {
            if(!slot.Child.IsVisible) continue;

            var childBoxToSurface = slot.Child.BoxToParent() * contentToSurface;

            var aabb = slot.Child.ComputeAABB(childBoxToSurface);

            if (!Rect2D.IntersectsWith(clipRect, aabb)) continue;

            _toCollect.Add((slot.Child, childBoxToSurface));
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
        foreach (var slot in GetActiveSlots()) slot.Child.Update(deltaTime);
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
        foreach (var slot in GetActiveSlots())
            if (slot.Child.IsLayoutValid)
                slot.Child.InvalidateLayout();
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return ArrangeContent(availableSpace);
    }


    private readonly List<(ISlot Slot, Matrix4x4 Transform)> _computeHitTestableSlotsForEventScratch = [];
    protected virtual (ISlot Slot, Matrix4x4 Transform)[] ComputeHitTestableSlotsForEvent(IPositionalEvent e, Matrix4x4 contentToSurface)
    {
        if (!IsChildrenHitTestable) return [];
        var slots = GetActiveSlots();
        if (e.ReverseTestOrder)
        {
            for (var i = slots.Length - 1; i > -1; i--)
            {
                var slot = slots[i];
                if(!slot.Child.IsHitTestable) continue;
                var childBoxToSurface = slot.Child.BoxToParent() * contentToSurface;
                if (slot.Child.PointWithin(childBoxToSurface, e.Position))
                {
                    _computeHitTestableSlotsForEventScratch.Add(new(slot, childBoxToSurface));
                }
            }
        }
        else
        {
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if(!slot.Child.IsHitTestable) continue;
                var childBoxToSurface = slot.Child.BoxToParent() * contentToSurface;
                if (slot.Child.PointWithin(childBoxToSurface, e.Position))
                {
                    _computeHitTestableSlotsForEventScratch.Add(new(slot, childBoxToSurface));
                }
            }
        }

        var result = _computeHitTestableSlotsForEventScratch.ToArray();
        _computeHitTestableSlotsForEventScratch.Clear();
        return result;
    }

    /// <summary>
    ///     Arranges content and returns their computed total size i.e. the combined length of all items in a list
    /// </summary>
    protected abstract Vector2 ArrangeContent(in Vector2 availableSpace);
}