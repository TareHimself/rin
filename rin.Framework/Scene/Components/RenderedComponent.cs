using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Entities;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components;

public abstract class RenderedComponent : SceneComponent
{
    [PublicAPI]
    public bool Visible { get; set; } = true;
    
    protected abstract void CollectSelf(DrawCommands drawCommands, Mat4 transform);
    public override void Collect(DrawCommands drawCommands, Mat4 parentTransform)
    {
        var myTransform = parentTransform * GetRelativeTransform();
        if(Visible) CollectSelf(drawCommands, myTransform);
        foreach (var attachedComponent in GetAttachedComponents())
        {
            attachedComponent.Collect(drawCommands, myTransform);
        }
    }
}