using System.Numerics;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics;

public readonly record struct StaticMeshProxyDesc
{
    public required IMesh Mesh { get; init; }
    public required int[] SurfaceIndices { get; init; }
    public required IMeshMaterial[] Materials { get; init; }
    public required Matrix4x4 Transform { get; init; }
}
