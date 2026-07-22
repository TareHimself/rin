using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Components.Lights;

namespace rin.Examples.SceneTest.entities;

public class CameraActor : Actor
{
    private readonly CameraComponent _camera;

    public CameraActor()
    {
        RootComponent = _camera = new CameraComponent();
        var light = AddComponent<PointLightComponent>();
        light.Radiance = 20.0f;
        light.AttachTo(RootComponent);
    }

    public CameraComponent GetCameraComponent()
    {
        return _camera;
    }
}