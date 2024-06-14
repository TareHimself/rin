using aerox.Runtime.Math;
using aerox.Runtime.Scene.Graphics;

namespace aerox.Runtime.Scene.Components.Lights;

public class PointLightComponent : LightComponent
{
    public override SceneLight ToSceneLight()
    {
        return new SceneLight()
        {
            Color = new Vector4<float>(Color.X,Color.Y,Color.Z,Intensity),
            Direction = 0.0f,
            Location = new Vector4<float>(GetWorldTransform().Location, 1.0f)
        };
    }
}