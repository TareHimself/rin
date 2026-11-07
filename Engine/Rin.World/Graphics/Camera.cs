namespace Rin.World.Graphics;

/// <summary>
///     Projection parameters of a camera entity. Its position and rotation come from <see cref="Transforms.WorldTransform" />.
/// </summary>
public struct Camera() : IComponentData
{
    /// <summary>
    ///     Vertical field of view, in degrees.
    /// </summary>
    public float FieldOfView = 90.0f;

    public float NearClipPlane = 0.01f;

    /// <summary>
    ///     How far the view frustum reaches for culling. The projection is infinite reversed-Z, so it does not clip.
    /// </summary>
    public float FarClipPlane = 50.0f;
}
