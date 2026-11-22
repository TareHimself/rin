using System.Numerics;
using Rin.Core.Views;

namespace Rin.World.Graphics;

/// <summary>
///     A light at the entity's world position.
/// </summary>
/// <remarks>
///     <see cref="RenderFeature" /> pushes the light when its change tick or its <see cref="Transforms.WorldTransform" />
///     tick is newer than the last extract.
/// </remarks>
public struct PointLight() : IComponentData
{
    public const float CutoffLux = 0.01f;

    public Color Color = Color.White;

    public float Lumens = 800.0f;

    /// <summary>
    ///     How far the light reaches. At zero, it reaches until it gives less than <see cref="CutoffLux" />.
    /// </summary>
    public float Radius;

    public readonly float EffectiveRadius => Radius > 0f ? Radius : MathF.Sqrt(Candela / CutoffLux);

    private readonly float Candela => Lumens / (4f * MathF.PI);

    internal readonly LightInfo ToLightInfo(in Matrix4x4 world) => new()
    {
        Color = Color,
        Direction = Vector3.Zero,
        Intensity = Candela,
        Radius = EffectiveRadius,
        LightType = LightType.Point,
        Location = world.Translation
    };
}
