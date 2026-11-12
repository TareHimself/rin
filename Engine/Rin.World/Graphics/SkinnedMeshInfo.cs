using System.Numerics;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Graphics;

/// <summary>
///     Contains info about a skinned mesh to be drawn in the scene
/// </summary>
public class SkinnedMeshInfo
{
    public required SkeletalPose Pose;
    public required Skeleton Skeleton;
    public required Matrix4x4 Transform;
    public required int[] SurfaceIndices { get; init; }
    public required IMeshMaterial[] Materials { get; init; }
    public required IMesh Mesh { get; init; }
}