using System.Numerics;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     Lights the scene from an environment map. With a zero <see cref="Extent" /> it reaches everywhere, like a sky;
///     otherwise it is a box around the entity that takes over for views inside it.
/// </summary>
/// <remarks>
///     <see cref="RenderFeature" /> pushes the light when its change tick or its <see cref="Transforms.WorldTransform" />
///     tick is newer than the last extract.
/// </remarks>
public struct EnvironmentLight() : IComponentData
{
    /// <summary>
    ///     A map from <see cref="RenderFeature.Environments" />.
    /// </summary>
    public EnvironmentMapHandle Map;

    public float Intensity = 1f;

    /// <summary>
    ///     The box's half-size in the entity's local space. Zero means unbounded.
    /// </summary>
    public Vector3 Extent;

    /// <summary>
    ///     How far inside the box's border a view must be before this light has fully taken over.
    /// </summary>
    public float BlendDistance;

    /// <summary>
    ///     The higher priority wins where boxes overlap, and among unbounded lights.
    /// </summary>
    public int Priority;
}
