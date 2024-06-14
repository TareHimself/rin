using aerox.Runtime.Math;
using aerox.Runtime.Scene.Components;
using aerox.Runtime.Scene.Components.Lights;

namespace aerox.Runtime.Scene.Entities.Lights;

public class PointLight : Entity
{
    public float Intensity
    {
        get => ((PointLightComponent?)RootComponent)?.Intensity ?? 0.0f;
        set {
            if (RootComponent is PointLightComponent pointLightComponent)
            {
                pointLightComponent.Intensity = value;
            }}
    }
    public float Radius 
    {
        get => ((PointLightComponent?)RootComponent)?.Radius ?? 0.0f;
        set {
            if (RootComponent is PointLightComponent pointLightComponent)
            {
                pointLightComponent.Radius = value;
            }}
    }
    public Vector4<float> Color 
    {
        get => ((PointLightComponent?)RootComponent)?.Color?? 0.0f;
        set {
            if (RootComponent is PointLightComponent pointLightComponent)
            {
                pointLightComponent.Color = value;
            }}
    }
    protected override SceneComponent CreateRootComponent() => new PointLightComponent();
}