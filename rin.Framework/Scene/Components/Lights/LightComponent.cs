using rin.Framework.Core.Math;
using rin.Framework.World.Components;

namespace rin.Framework.Scene.Components.Lights;

public abstract class LightComponent : SceneComponent
{
    public float Intensity = 0.0f;
    public float Radius = 0.0f;
    public Vector4<float> Color = 1.0f;
    

    public override void Init()
    {
        base.Init();
    }

}