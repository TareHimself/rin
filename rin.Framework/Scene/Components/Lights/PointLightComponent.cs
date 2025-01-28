using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components.Lights;

public class PointLightComponent : LightComponent
{
    public override void Collect(DrawCommands drawCommands, Mat4 parentTransform)
    {
        var loc = parentTransform * new Vec4<float>(GetRelativeLocation(), 1.0f);
        drawCommands.AddLight(new LightInfo
        {
            Color = Color,
            Direction = new Vec3<float>(0.0f),
            Radiance = Radiance,
            LightType = LightType.Point,
            Location = new Vec3<float>(loc.X, loc.Y, loc.Z),
        });
        base.Collect(drawCommands, parentTransform);
    }
}