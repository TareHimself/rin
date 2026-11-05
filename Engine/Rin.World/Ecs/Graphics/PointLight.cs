using System.Numerics;
using Rin.Core.Views;
using Rin.World.Graphics;

namespace Rin.World.Ecs.Graphics;

/// <summary>
///     A light at the entity's world position.
/// </summary>
/// <remarks>
///     Every setter marks the light dirty, so <see cref="RenderFeature" /> pushes a change written through a query span.
/// </remarks>
public struct PointLight() : IComponentData
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

    public float Radiance
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 5.0f;

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
        Direction = Vector3.Zero,
        Radiance = Radiance,
        Radius = Radius,
        LightType = LightType.Point,
        Location = world.Translation
    };
}
