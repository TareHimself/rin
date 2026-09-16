using System.Numerics;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Graphics;

public readonly record struct SkinnedMeshProxyDesc
{
    public required IMesh Mesh { get; init; }
    public required Skeleton Skeleton { get; init; }
    public required Pose Pose { get; init; }
    public required int[] SurfaceIndices { get; init; }
    public required IMeshMaterial[] Materials { get; init; }
    public required Matrix4x4 Transform { get; init; }
}
