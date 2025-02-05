using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Editor.Scene.Graphics;

namespace rin.Editor.Scene.Components;

public abstract class RenderedComponent : SceneComponent
{
   
    
    protected abstract void CollectSelf(DrawCommands drawCommands, Mat4 transform);
    public override void Collect(DrawCommands drawCommands, Mat4 parentTransform)
    {
        
    }
}