using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Graphics;

namespace Rin.World.Components.Lights;

public class DirectionalLightComponent : LightComponent
{
    public override void Collect(CommandList commandList, Matrix4x4 parentTransform)
    {
        var sceneTransform = Transform.From(GetTransform().ToMatrix() * parentTransform);
        commandList.AddLight(new LightInfo
        {
            Color = Color,
            Direction = sceneTransform.Orientation.GetForward(),
            Radiance = Radiance,
            LightType = LightType.Directional,
            Location = sceneTransform.Position
        });
        base.Collect(commandList, parentTransform);
    }
}