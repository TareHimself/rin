using System.Numerics;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Graphics;

/// <summary>
///     Contains info about a static mesh to be drawn in the scene
/// </summary>
public class SkinnedMeshInfo : IMeshCommand
{
    public required Pose Pose;
    public required Skeleton Skeleton;
    public required Matrix4x4 Transform;
    public bool CastShadow { get; set; }
    public required int[] SurfaceIndices { get; init; }
    public required IMeshMaterial[] Materials { get; init; }
    public required IMesh Mesh { get; init; }
}