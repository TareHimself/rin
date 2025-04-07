namespace Rin.Engine.World.Components;

public class CameraComponent : SceneComponent
{
    public float FarClipPlane = 50.0f;
    public float FieldOfView = 90.0f;

    public float NearClipPlane = 0.01f;
}