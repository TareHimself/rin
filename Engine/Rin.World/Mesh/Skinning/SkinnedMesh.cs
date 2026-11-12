using Rin.World.Graphics.Mesh;

namespace Rin.World.Mesh.Skinning;

public class SkinnedMesh
{
    public required Skeleton Skeleton { get; init; }
    public required MeshHandle Mesh { get; init; }
}
