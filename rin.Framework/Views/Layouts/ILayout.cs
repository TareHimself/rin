using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Enums;

namespace rin.Framework.Views.Layouts;

public interface ILayout : IDisposable
{
    public CompositeView Container { get; }
    protected ISlot MakeSlot(View view);
    
    public void OnSlotUpdated(ISlot slot);
    
    public Vector2 Apply(Vector2 availableSpace);
    
    public Vector2 ComputeDesiredContentSize();
}