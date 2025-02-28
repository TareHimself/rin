using Rin.Engine.Core.Math;

namespace Rin.Editor.Scene.Graphics;

public class MeshInfo
{
    public required DeviceGeometry Geometry;
    public required MeshSurface Surface;
    public required Mat4 Transform;
}