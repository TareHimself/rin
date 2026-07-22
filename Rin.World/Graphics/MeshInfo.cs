using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics;

public class MeshInfo
{
    public required DeviceBufferView IndexBuffer;
    public required MeshSurface Surface;
    public required Matrix4x4 Transform;
    public required DeviceBufferView VertexBuffer;
}