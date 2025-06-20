using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Meshes;

namespace Rin.Engine.World.Graphics;

public class MeshInfo
{
    public required DeviceBufferView IndexBuffer;
    public required MeshSurface Surface;
    public required Matrix4x4 Transform;
    public required DeviceBufferView VertexBuffer;
}