using System.Numerics;
using Rin.Engine.Graphics.Meshes;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Contains info about a static mesh to be drawn in the scene
/// </summary>
public class SkinnedMeshInfo : ICommand
{
    public required Matrix4x4 Transform;

    public bool CastShadows { get; set; }
    public required IMeshMaterial Material { get; init; }
    public required IMesh Mesh { get; init; }
    public required int SurfaceIndex { get; init; }
}