using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.World.Graphics;

namespace Rin.Engine.World.Components.Lights;

public class PointLightComponent : LightComponent
{
    public override void Collect(DrawCommands drawCommands, Matrix4x4 parentTransform)
    {
        var loc = GetLocation().Transform(parentTransform);
        drawCommands.AddLight(new LightInfo
        {
            Color = Color,
            Direction = new Vector3(0.0f),
            Radiance = Radiance,
            LightType = LightType.Point,
            Location = new Vector3(loc.X, loc.Y, loc.Z),
        });
        base.Collect(drawCommands, parentTransform);
    }
}