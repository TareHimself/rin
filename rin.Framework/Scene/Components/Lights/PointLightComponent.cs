using rin.Framework.Core.Math;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components.Lights;

public class PointLightComponent : LightComponent
{
    
    // public override SceneLight ToSceneLight()
    // {
    //     return new SceneLight()
    //     {
    //         Color = new Vector4<float>(Color.X,Color.Y,Color.Z,Intensity),
    //         Direction = 0.0f,
    //         Location = new Vector4<float>(GetWorldTransform().Location, 1.0f)
    //     };
    // }

    public override void Collect(DrawCommands drawCommands, Matrix4 parentTransform)
    {
        base.Collect(drawCommands, parentTransform);
        Transform myTransform = parentTransform * GetRelativeTransform();
        drawCommands.AddLight(new DeviceLight()
        {
            Location = new Vector4<float>(myTransform.Location, 0.0f),
            Color = Color,
            Direction = 0.0f
        });
    }
}