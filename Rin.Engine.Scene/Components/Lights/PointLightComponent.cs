using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Scene.Graphics;

namespace Rin.Engine.Scene.Components.Lights;

public class PointLightComponent : LightComponent
{
    public override void Collect(DrawCommands drawCommands, Mat4 parentTransform)
    {
        var loc = parentTransform * new Vector4(GetRelativeLocation(), 1.0f);
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