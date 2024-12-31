using rin.Runtime.Core.Math;
using rin.Scene.Graphics;

namespace rin.Scene.Components;

public abstract class RenderedComponent : TransformComponent
{
    protected override void CollectSelf(SceneFrame frame, Matrix4 parentTransform, Matrix4 myTransform)
    {
        base.CollectSelf(frame, parentTransform, myTransform);
    }
}