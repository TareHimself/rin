using JetBrains.Annotations;

namespace Rin.Engine.Graphics.Meshes;

public class DeviceMesh : IMesh, IDisposable
{
    [PublicAPI] public Bounds3D Bounds;

    [PublicAPI] public IDeviceBuffer IndexBuffer;

    [PublicAPI] public MeshSurface[] Surfaces;

    [PublicAPI] public IDeviceBuffer VertexBuffer;

    public DeviceMesh(IDeviceBuffer vertexBuffer, IDeviceBuffer indexBuffer, MeshSurface[] surfaces)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        Surfaces = surfaces;
        Bounds = Surfaces.Aggregate(Surfaces.First().Bounds, (t, c) => t + c.Bounds);
    }

    public void Dispose()
    {
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }

    public MeshSurface[] GetSurfaces()
    {
        return Surfaces;
    }

    public MeshSurface GetSurface(int surfaceIndex)
    {
        return Surfaces[surfaceIndex];
    }

    public IDeviceBufferView GetVertices()
    {
        return VertexBuffer.GetView();
    }

    public IDeviceBufferView GetVertices(int surfaceIndex)
    {
        var surface = Surfaces[surfaceIndex];
        return VertexBuffer.GetView(surface.VertexStart * sizeof(uint), surface.VertexCount * sizeof(uint));
    }

    public IDeviceBufferView GetIndices()
    {
        return IndexBuffer.GetView();
    }

    public Bounds3D GetBounds()
    {
        return Bounds;
    }
}