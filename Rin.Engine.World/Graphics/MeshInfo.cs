using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Meshes;
using Rin.Framework.Graphics.Vulkan.Meshes;

namespace Rin.Engine.World.Graphics;

public class MeshInfo
{
    public required DeviceBufferView IndexBuffer;
    public required MeshSurface Surface;
    public required Matrix4x4 Transform;
    public required DeviceBufferView VertexBuffer;
}