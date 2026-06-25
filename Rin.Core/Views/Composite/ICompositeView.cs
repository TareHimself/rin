using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Views.Layouts;

namespace Rin.Core.Views.Composite;

public interface ICompositeView : IView
{
    public Clip Clip { get; set; }

    /// <summary>
    ///     Compute all the offsets applied to a child of this <see cref="CompositeView" />
    /// </summary>
    /// <param name="child"></param>
    /// <returns></returns>
    public Matrix4x4 ComputeChildOffsets(IView child);

    [PublicAPI]
    public IEnumerable<ISlot> GetSlots();

    [PublicAPI]
    public IEnumerable<ISlot> GetCollectableSlots();

    [PublicAPI]
    public IEnumerable<ISlot> GetHitTestableSlots();


    [PublicAPI]
    public void OnChildLayoutInvalidated(IView child);

    [PublicAPI]
    public void OnChildAdded(IView child);

    [PublicAPI]
    public void OnChildRemoved(IView child);

    /// <summary>
    ///     Called during the layout pass when a child needs layout but this view does not.
    ///     Flowing containers (lists, flex) should not need to override this — bubbling via
    ///     <see cref="OnChildLayoutInvalidated" /> queues the parent, whose pass covers the child.
    /// </summary>
    [PublicAPI]
    public void OnChildNeedsLayout(IView child);
}