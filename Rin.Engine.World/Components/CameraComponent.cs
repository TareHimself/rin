namespace Rin.Engine.World.Components;

public class CameraComponent : SceneComponent
{
    public float FieldOfView = 90.0f;

    public float NearClipPlane = 1f;

    public float FarClipPlane = 1000.0f;
}