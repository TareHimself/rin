using rin.Editor.Scene.Actors;
using rin.Editor.Scene.Components;
using rin.Editor.Scene.Components.Lights;

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