using System.Numerics;
using Rin.Shade;

namespace Rin.World.Graphics.Surfaces;

[ShadeExport]
public struct SurfaceInput
{
    public Vector2 Uv;
    public Vector3 WorldPosition;
    public Vector3 WorldNormal;
}
