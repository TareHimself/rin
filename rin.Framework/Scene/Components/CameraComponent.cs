using rin.Framework.Scene.Entities;

namespace rin.Framework.Scene.Components;

[Component(typeof(TransformComponent))]
public class CameraComponent(Entity owner) : Component(owner)
{
    public float FieldOfView = 90.0f;

    public float NearClipPlane = 1f;

    public float FarClipPlane = 1000.0f;
}