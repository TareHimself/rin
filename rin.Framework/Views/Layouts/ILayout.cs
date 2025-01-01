using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

public interface ILayout : IDisposable
{
    public CompositeView Container { get; }
    protected ISlot MakeSlot(View widget);
    
    public void OnSlotUpdated(ISlot slot);
    
    public Vec2<float> Apply(Vec2<float> availableSpace);
    
    public Vec2<float> ComputeDesiredContentSize();
}