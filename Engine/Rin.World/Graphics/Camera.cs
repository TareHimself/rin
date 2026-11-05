namespace Rin.World.Graphics;

/// <summary>
///     Projection parameters of a camera entity. Its position and rotation come from <see cref="Transforms.WorldTransform" />.
/// </summary>
public struct Camera() : IComponentData
{
    public float FieldOfView = 90.0f;
    public float NearClipPlane = 0.01f;
    public float FarClipPlane = 50.0f;
}
