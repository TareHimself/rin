using System.Numerics;
using JetBrains.Annotations;
using Rin.Editor.Scene.Graphics;
using Rin.Engine.Core.Math;

namespace Rin.Editor.Scene.Components.Lights;

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