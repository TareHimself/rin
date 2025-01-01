using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Entities;

namespace rin.Framework.Scene.Components.Lights;

[Component(typeof (TransformComponent))]
public abstract class LightComponent(Entity owner) : Component(owner)
{
    [PublicAPI]
    public float Intensity  { get; set; }
    [PublicAPI]
    public float Radius { get; set; }
    [PublicAPI]
    public Vec4<float> Color { get; set; }
}