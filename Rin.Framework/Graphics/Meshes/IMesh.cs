
using Rin.Framework.Shared;

namespace Rin.Framework.Graphics.Meshes;

public interface IMesh
{
    public ulong GetVertexFormatSize();

    public MeshSurface[] GetSurfaces();
    public MeshSurface GetSurface(int surfaceIndex);
    public DeviceBufferView GetVertices();
    public DeviceBufferView GetVertices(int surfaceIndex);
    public uint GetVertexCount();
    public uint GetVertexCount(int surfaceIndex);

    public DeviceBufferView GetIndices();

    public Bounds3D GetBounds();
}