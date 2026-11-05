using Rin.World.Graphics;
using Rin.World.Math;

namespace Rin.World.Components;

public class CameraComponent : WorldComponent
{
    public float FarClipPlane = 50.0f;
    public float FieldOfView = 90.0f;

    public float NearClipPlane = 0.01f;

    public CameraView ToCameraView() =>
        new(GetTransform(Space.World).ToMatrix(), FieldOfView, NearClipPlane, FarClipPlane);
}