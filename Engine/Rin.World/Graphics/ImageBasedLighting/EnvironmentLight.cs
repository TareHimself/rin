using System.Numerics;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     Lights the scene from an environment map. With a zero <see cref="Extent" /> it reaches everywhere, like a sky;
///     otherwise it is a box around the entity that takes over for views inside it.
/// </summary>
/// <remarks>
///     Every setter marks the light dirty, so <see cref="RenderFeature" /> pushes a change written through a query span.
/// </remarks>
public struct EnvironmentLight() : IComponentData
{
    /// <summary>
    ///     A map from <see cref="RenderFeature.Environments" />.
    /// </summary>
    public EnvironmentMapHandle Map
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public float Intensity
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = 1f;

    /// <summary>
    ///     The box's half-size in the entity's local space. Zero means unbounded.
    /// </summary>
    public Vector3 Extent
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     How far inside the box's border a view must be before this light has fully taken over.
    /// </summary>
    public float BlendDistance
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     The higher priority wins where boxes overlap, and among unbounded lights.
    /// </summary>
    public int Priority
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public bool IsDirty { get; internal set; } = true;
}
