using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;

namespace Rin.World.Graphics;

/// <summary>
///     A light shining along the entity's world forward axis.
/// </summary>
/// <remarks>
///     Every setter marks the light dirty, so <see cref="RenderFeature" /> pushes a change written through a query chunk.
/// </remarks>
public struct DirectionalLight() : IComponentData
{
    public Color Color
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = Color.White;

    public float Lux
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 100000.0f;

    public float Radius
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 10000.0f;

    public bool IsDirty { get; internal set; } = true;

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
