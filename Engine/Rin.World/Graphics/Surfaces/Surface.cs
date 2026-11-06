using System.Numerics;
using Rin.Shade;

namespace Rin.World.Graphics.Surfaces;

[ShadeExport]
public struct Surface
{
    public Vector3 BaseColor;
    public Vector3 Normal;
    public float Roughness;
    public float Metallic;
    public float Specular;
    public float Emissive;
}
