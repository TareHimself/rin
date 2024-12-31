using rin.Core.Math;
using rin.Scene.Components;
using rin.Scene.Components.Lights;
using rin.Scene.Entities;
using rin.Runtime.Widgets;

namespace SceneTest.entities;

public class CameraEntity : Entity
{
    public CameraComponent? CameraComp;
    // protected override SceneComponent CreateRootComponent()
    // {
    //     
    //     return CameraComp;
    // }


    protected override void OnTick(double deltaSeconds)
    {
        base.OnTick(deltaSeconds);
        // var root = RootComponent;
        // if(RootComponent == null) return;
        //
        // RootComponent.SetRelativeRotation(RootComponent.GetRelativeRotation().ApplyYaw(20.0f * (float)deltaSeconds));
    }

    protected override void CreateDefaultComponents(TransformComponent root)
    {
        base.CreateDefaultComponents(root);
        CameraComp = new CameraComponent();
        AddComponent(CameraComp).AttachTo(root);
        CameraComp.SetRelativeLocation(z: -5.0f);
        var light = AddComponent(new PointLightComponent()
        {
            Intensity = 1.0f,
            Color = Color.White,
            Radius = 200000.0f,
        });
        light.SetRelativeLocation(z: - 5.0f);
    }
}