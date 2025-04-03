using System.Numerics;
using Rin.Engine.Math;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Math;

namespace Rin.Engine.World.Components.Lights;

public class DirectionalLightComponent : LightComponent
{
    public override void Collect(DrawCommands drawCommands, Matrix4x4 parentTransform)
    {
        var sceneTransform = Transform.From(GetTransform().ToMatrix() * parentTransform);
        drawCommands.AddLight(new LightInfo
        {
            Color = Color,
            Direction = sceneTransform.Rotation.GetForward(),
            Radiance = Radiance,
            LightType = LightType.Directional,
            Location = sceneTransform.Location
        });
        base.Collect(drawCommands, parentTransform);
    }
}