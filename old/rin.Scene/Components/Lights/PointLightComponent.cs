
using rin.Framework.Core.Math;
using rin.Scene.Graphics;

namespace rin.Scene.Components.Lights;

public class PointLightComponent : LightComponent
{
    public override SceneLight ToSceneLight()
    {
        return new SceneLight()
        {
            Color = new Vec4<float>(Color.X,Color.Y,Color.Z,Intensity),
            Direction = 0.0f,
            Location = new Vec4<float>(GetWorldTransform().Location, 1.0f)
        };
    }
}