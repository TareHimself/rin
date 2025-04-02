using System.Numerics;
using Rin.Engine.Graphics.Meshes;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Base class for all draws involving <see cref="DeviceGeometry" />
/// </summary>
public class GeometryInfo : ICommand
{
    public required Matrix4x4 Transform;
    public bool CastShadows { get; set; }
    public required IMeshMaterial MeshMaterial { get; set; }
    public required IMesh Mesh { get; set; }
    public required int SurfaceIndex { get; set; }
}