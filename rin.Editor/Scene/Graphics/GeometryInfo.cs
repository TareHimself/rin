using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;

namespace rin.Editor.Scene.Graphics;

/// <summary>
/// Base class for all draws involving <see cref="DeviceGeometry"/>
/// </summary>
public class GeometryInfo : ICommand
{
    public bool CastShadows { get; set; }
    public required IMeshMaterial MeshMaterial { get; set; }
    public required DeviceGeometry Geometry { get; set; }
    public required MeshSurface Surface;
    public required Mat4 Transform;
}