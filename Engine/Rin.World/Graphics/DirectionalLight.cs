using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;

namespace Rin.World.Graphics;

/// <summary>
///     A light shining along the entity's world forward axis.
/// </summary>
/// <remarks>
///     <see cref="RenderFeature" /> pushes the light when its change tick or its <see cref="Transforms.WorldTransform" />
///     tick is newer than the last extract.
/// </remarks>
public struct DirectionalLight() : IComponentData
{
    public Color Color = Color.White;

    public float Lux = 100000.0f;

    public float Radius = 10000.0f;

    internal readonly LightInfo ToLightInfo(in Matrix4x4 world) => new()
    {
        Color = Color,
        Direction = Transform.From(world).Orientation.GetForward(),
        Intensity = Lux,
        Radius = Radius,
        LightType = LightType.Directional,
        Location = world.Translation
    };
}
