namespace Rin.Engine.World.Components;

public class CameraComponent : SceneComponent
{
    public float FarClipPlane = 1000.0f;
    public float FieldOfView = 90.0f;

    public float NearClipPlane = 1f;
}