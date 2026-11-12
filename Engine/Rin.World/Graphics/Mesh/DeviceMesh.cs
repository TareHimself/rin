using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.World.Graphics.Mesh;

public class DeviceMesh : IMesh, IDisposable
{
    private readonly ulong _formatSize;
    [PublicAPI] public Bounds3D Bounds;

    [PublicAPI] public DeviceBufferView IndexBuffer;

    [PublicAPI] public MeshSurface[] Surfaces;

    [PublicAPI] public DeviceBufferView VertexBuffer;

    public DeviceMesh(DeviceBufferView vertexBuffer, DeviceBufferView indexBuffer, MeshSurface[] surfaces,
        ulong vertexFormatSize)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        Surfaces = surfaces;
        Bounds = Surfaces.Aggregate(Surfaces.First().Bounds, (t, c) => t + c.Bounds);
        _formatSize = vertexFormatSize;
    }

    public void Dispose()
    {
        IGraphicsModule.Get().FreeResourceHandles(VertexBuffer.Buffer, IndexBuffer.Buffer);
    }

    public MeshSurface[] GetSurfaces()
    {
        return Surfaces;
    }

    public MeshSurface GetSurface(int surfaceIndex)
    {
        return Surfaces[surfaceIndex];
    }

    public DeviceBufferView GetVertices()
    {
        return VertexBuffer;
    }

    public DeviceBufferView GetVertices(int surfaceIndex)
    {
        var surface = Surfaces[surfaceIndex];
        return VertexBuffer.GetView(surface.VertexStart * _formatSize, surface.VertexCount * _formatSize);
    }

    public uint GetVertexCount(int surfaceIndex)
    {
        return GetSurface(surfaceIndex).VertexCount;
    }

    public DeviceBufferView GetIndices()
    {
        return IndexBuffer;
    }

    public Bounds3D GetBounds()
    {
        return Bounds;
    }
}