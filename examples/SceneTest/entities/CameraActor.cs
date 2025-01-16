using rin.Framework.Scene.Actors;
using rin.Framework.Scene.Components;
using rin.Framework.Scene.Components.Lights;

namespace SceneTest.entities;

public class CameraActor : Actor
{
    private readonly CameraComponent _camera;
    
    public CameraComponent GetCameraComponent() => _camera;
    
    public CameraActor()
    {
        RootComponent = _camera = AddComponent<CameraComponent>();
        var light = AddComponent<DirectionalLightComponent>();
        light.Radiance = 10.0f;
        light.AttachTo(RootComponent);
    }
}