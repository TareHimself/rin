using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;

namespace Rin.Editor.Scene.Graphics;

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