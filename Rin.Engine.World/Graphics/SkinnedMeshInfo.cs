using System.Numerics;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.World.Mesh.Skinning;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Contains info about a static mesh to be drawn in the scene
/// </summary>
public class SkinnedMeshInfo : IMeshCommand
{
    public required Matrix4x4 Transform;
    public required Pose Pose;
    public required Skeleton Skeleton;
    public bool CastShadow { get; set; }
    public required int[] SurfaceIndices { get; init; }
    public required IMeshMaterial[] Materials { get; init; }
    public required IMesh Mesh { get; init; }
}