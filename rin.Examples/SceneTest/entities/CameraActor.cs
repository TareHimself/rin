using Rin.Engine.Scene.Actors;
using Rin.Engine.Scene.Components;
using Rin.Engine.Scene.Components.Lights;

namespace rin.Examples.SceneTest.entities;

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