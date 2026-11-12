using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.World.Graphics.Mesh;

public interface IMesh
{
    public MeshSurface[] GetSurfaces();
    public MeshSurface GetSurface(int surfaceIndex);
    public DeviceBufferView GetVertices();
    public DeviceBufferView GetVertices(int surfaceIndex);
    public uint GetVertexCount(int surfaceIndex);

    public DeviceBufferView GetIndices();

    public Bounds3D GetBounds();
}
