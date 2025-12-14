using System.Numerics;
using Rin.Engine.World.Graphics;
using Rin.Framework.Shared.Math;

namespace Rin.Engine.World.Components.Lights;

public class PointLightComponent : LightComponent
{
    public override void Collect(CommandList commandList, Matrix4x4 parentTransform)
    {
        var loc = GetLocation().Transform(parentTransform);
        commandList.AddLight(new LightInfo
        {
            Color = Color,
            Direction = new Vector3(0.0f),
            Radiance = Radiance,
            LightType = LightType.Point,
            Location = new Vector3(loc.X, loc.Y, loc.Z)
        });
        base.Collect(commandList, parentTransform);
    }
}