using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

public interface ILayout : IDisposable
{
    public CompositeView Container { get; }
    protected ISlot MakeSlot(View widget);
    
    public void OnSlotUpdated(ISlot slot);
    
    public Vector2<float> Apply(Vector2<float> availableSpace);
    
    public Vector2<float> ComputeDesiredContentSize();
}