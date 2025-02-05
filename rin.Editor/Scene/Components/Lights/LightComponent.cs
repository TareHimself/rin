using System.Numerics;
using JetBrains.Annotations;
using rin.Framework.Core.Math;

namespace rin.Editor.Scene.Components.Lights;


public abstract class LightComponent : SceneComponent
{
    [PublicAPI] 
    public float Radiance { get; set; } = 5.0f;

    [PublicAPI] public float Radius { get; set; } = 10000.0f;
    [PublicAPI] public Vector3 Color { get; set; } = new Vector3(1.0f);

}