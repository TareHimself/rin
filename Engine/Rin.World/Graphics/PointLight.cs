using System.Numerics;
using Rin.Core.Views;

namespace Rin.World.Graphics;

/// <summary>
///     A light at the entity's world position.
/// </summary>
/// <remarks>
///     Every setter marks the light dirty, so <see cref="RenderFeature" /> pushes a change written through a query span.
/// </remarks>
public struct PointLight() : IComponentData
{
    public const float CutoffLux = 0.01f;

    public Color Color
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = Color.White;

    public float Lumens
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 800.0f;

    /// <summary>
    ///     How far the light reaches. Unset, it reaches until it gives less than <see cref="CutoffLux" />.
    /// </summary>
    public float? Radius
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public readonly float EffectiveRadius => Radius ?? MathF.Sqrt(Candela / CutoffLux);

    private readonly float Candela => Lumens / (4f * MathF.PI);

    public bool IsDirty { get; internal set; } = true;

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
