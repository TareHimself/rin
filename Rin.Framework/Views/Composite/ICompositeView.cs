using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Layouts;

namespace Rin.Framework.Views.Composite;

public interface ICompositeView : IView
{
    public Clip Clip { get; set; }

    /// <summary>
    /// Compute all the offsets applied to a child of this <see cref="CompositeView"/>
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
    public void OnChildInvalidated(IView child, InvalidationType invalidation);

    [PublicAPI]
    public void OnChildAdded(IView child);

    [PublicAPI]
    public void OnChildRemoved(IView child);
    
}