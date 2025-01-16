using rin.Framework.Core.Math;

namespace rin.Framework.Scene.Graphics;

public class MeshInfo
{
    public required DeviceGeometry Geometry;
    public required MeshSurface Surface;
    public required Mat4 Transform;
}