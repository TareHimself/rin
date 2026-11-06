using System.Numerics;
using Rin.Shade;

namespace Rin.World.Graphics.Surfaces;

/// <summary>
///     A vertex after the mesh transform: what <see cref="ISurfaceMaterial{TSelf}.ProcessVertex" /> receives and returns.
/// </summary>
[ShadeExport]
public struct WorldVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 Uv;
}
