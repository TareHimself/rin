using JetBrains.Annotations;

namespace Rin.Framework.Graphics.Meshes;

public class DeviceMesh : IMesh, IDisposable
{
    private readonly ulong _formatSize;
    [PublicAPI] public Bounds3D Bounds;

    [PublicAPI] public IDeviceBuffer IndexBuffer;

    [PublicAPI] public MeshSurface[] Surfaces;

    [PublicAPI] public IDeviceBuffer VertexBuffer;

    public DeviceMesh(IDeviceBuffer vertexBuffer, IDeviceBuffer indexBuffer, MeshSurface[] surfaces,
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
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }

    public ulong GetVertexFormatSize()
    {
        return _formatSize;
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
        return VertexBuffer.GetView();
    }

    public DeviceBufferView GetVertices(int surfaceIndex)
    {
        var surface = Surfaces[surfaceIndex];
        var formatSize = GetVertexFormatSize();
        return VertexBuffer.GetView(surface.VertexStart * formatSize, surface.VertexCount * formatSize);
    }

    public uint GetVertexCount()
    {
        var vertices = GetVertices();
        return (uint)(vertices.Size / _formatSize);
    }

    public uint GetVertexCount(int surfaceIndex)
    {
        return GetSurface(surfaceIndex).VertexCount;
    }

    public DeviceBufferView GetIndices()
    {
        return IndexBuffer.GetView();
    }

    public Bounds3D GetBounds()
    {
        return Bounds;
    }
}