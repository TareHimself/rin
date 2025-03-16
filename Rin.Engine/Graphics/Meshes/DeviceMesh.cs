using JetBrains.Annotations;
using Rin.Engine.Core;
using Rin.Engine.Graphics.Descriptors;

namespace Rin.Engine.Graphics.Meshes;

public class DeviceMesh(IDeviceBuffer vertexBuffer, IDeviceBuffer indexBuffer,MeshSurface[] surfaces) : IDisposable
{
    [PublicAPI]
    public IDeviceBuffer VertexBuffer = vertexBuffer;
    [PublicAPI]
    public IDeviceBuffer IndexBuffer = indexBuffer;
    [PublicAPI]
    public MeshSurface[] Surfaces = surfaces;

    public void Dispose()
    {
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}