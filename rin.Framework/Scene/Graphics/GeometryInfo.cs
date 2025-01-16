using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;

namespace rin.Framework.Scene.Graphics;

/// <summary>
/// Base class for all draws involving <see cref="DeviceGeometry"/>
/// </summary>
public class GeometryInfo : ICommand
{
    public bool CastShadows { get; set; }
    public required IMaterial Material { get; set; }
    public required DeviceGeometry Geometry { get; set; }
    public required MeshSurface Surface;
    public required Mat4 Transform;
}