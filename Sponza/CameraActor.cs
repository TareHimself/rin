using System.Numerics;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Components.Lights;

namespace Sponza;

public class CameraActor : Actor
{
    private readonly CameraComponent _camera;

    public CameraActor()
    {
        RootComponent = _camera = new CameraComponent();
        var light = AddComponent<PointLightComponent>();
        light.Radiance = 0.0f;
        light.AttachTo(RootComponent);
        SetLocation(new Vector3(0, 100, 0));
    }

    public CameraComponent GetCameraComponent()
    {
        return _camera;
    }
}