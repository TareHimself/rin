using System.Numerics;
using Rin.Engine.Views.Composite;

namespace Rin.Engine.Views.Layouts;

public interface ILayout
{
    public CompositeView Container { get; }
    protected ISlot MakeSlot(View view);

    public void OnSlotUpdated(ISlot slot);

    public Vector2 Apply(Vector2 availableSpace);

    public Vector2 ComputeDesiredContentSize();
}