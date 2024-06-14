using aerox.Runtime.Math;
using aerox.Runtime.Scene.Graphics;

namespace aerox.Runtime.Scene.Components;

public abstract class RenderedComponent : SceneComponent
{
    protected override void CollectSelf(SceneFrame frame, Matrix4 parentTransform, Matrix4 myTransform)
    {
        base.CollectSelf(frame, parentTransform, myTransform);
    }
}