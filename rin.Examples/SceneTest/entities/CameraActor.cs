using Rin.Engine.World.Actors;
using Rin.Engine.World.Components;
using Rin.Engine.World.Components.Lights;

namespace rin.Examples.SceneTest.entities;

public class CameraActor : Actor
{
    private readonly CameraComponent _camera;

    public CameraActor()
    {
        RootComponent = _camera = new CameraComponent();
        var light = AddComponent<PointLightComponent>();
        light.Radiance = 1.0f;
        light.AttachTo(RootComponent);
    }

    public CameraComponent GetCameraComponent()
    {
        return _camera;
    }
}