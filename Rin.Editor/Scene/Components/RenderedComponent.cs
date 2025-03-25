using JetBrains.Annotations;
using Rin.Editor.Scene.Graphics;
using Rin.Engine.Core.Math;

namespace Rin.Editor.Scene.Components;

public abstract class RenderedComponent : SceneComponent
{
   
    
    protected abstract void CollectSelf(DrawCommands drawCommands, Mat4 transform);
    public override void Collect(DrawCommands drawCommands, Mat4 parentTransform)
    {
        
    }
}