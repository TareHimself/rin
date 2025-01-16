using JetBrains.Annotations;
using rin.Framework.Core.Math;

namespace rin.Framework.Scene.Components.Lights;


public abstract class LightComponent : SceneComponent
{
    [PublicAPI] 
    public float Radiance { get; set; } = 5.0f;

    [PublicAPI] public float Radius { get; set; } = 10000.0f;
    [PublicAPI] public Vec3<float> Color { get; set; } = new Vec3<float>(1.0f);

}