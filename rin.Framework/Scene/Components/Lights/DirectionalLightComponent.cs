using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components.Lights;

public class DirectionalLightComponent : LightComponent
{
    public override void Collect(DrawCommands drawCommands, Mat4 parentTransform)
    {
        Transform sceneTransform = parentTransform * GetRelativeTransform();
        drawCommands.AddLight(new LightInfo()
        {
            Color = Color,
            Direction = sceneTransform.Rotation.GetForwardVector(),
            Intensity = Radiance,
            LightType = LightType.Directional,
            Location = sceneTransform.Location,
        });
        base.Collect(drawCommands, parentTransform);
    }
}