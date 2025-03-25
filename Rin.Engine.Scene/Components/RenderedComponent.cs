using Rin.Engine.Core.Math;
using Rin.Engine.Scene.Graphics;

namespace Rin.Engine.Scene.Components;

public abstract class RenderedComponent : SceneComponent
{
   
    
    protected abstract void CollectSelf(DrawCommands drawCommands, Mat4 transform);
    public override void Collect(DrawCommands drawCommands, Mat4 parentTransform)
    {
        
    }
}