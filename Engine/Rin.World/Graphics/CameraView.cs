using System.Numerics;

namespace Rin.World.Graphics;

/// <summary>
///     What a render system needs to draw from a camera: its world matrix, projection parameters and what to show.
/// </summary>
public readonly struct CameraView(in Matrix4x4 world, float fieldOfView, float nearClipPlane, float farClipPlane)
{
    public Matrix4x4 World { get; } = world;

    public float FieldOfView { get; } = fieldOfView;

    public float NearClipPlane { get; } = nearClipPlane;

    public float FarClipPlane { get; } = farClipPlane;

    public RenderChannel Channel { get; init; }
}
