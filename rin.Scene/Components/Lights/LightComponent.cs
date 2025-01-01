using rin.Framework.Core.Math;
using rin.Scene.Graphics;

namespace rin.Scene.Components.Lights;

public abstract class LightComponent : RenderedComponent
{
    public float Intensity = 0.0f;
    public float Radius = 0.0f;
    public Vec4<float> Color = 1.0f;
    
    public abstract SceneLight ToSceneLight();

    public override void Start()
    {
        base.Start();
        Owner?.OwningScene.Lights.Add(this);
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        Owner?.OwningScene.Lights.Remove(this);
    }
}