using System.Numerics;
using Rin.Framework.Views.Composite;

namespace Rin.Framework.Views.Layouts;

public interface ILayout
{
    public ICompositeView Container { get; }
    protected ISlot MakeSlot(IView view);

    public void OnSlotUpdated(ISlot slot);

    public Vector2 Apply(in Vector2 availableSpace);

    public Vector2 ComputeDesiredContentSize();
}