using Rin.Core;
using Rin.Core.Shared.Buffers;

namespace Rin.World.Graphics.Mesh;

public interface IMeshFactory : IDisposable
{
    public Pair<int, Task> CreateMesh<TVertexFormat>(Buffer<TVertexFormat> vertices, Buffer<uint> indices,
        MeshSurface[] surfaces) where TVertexFormat : unmanaged, IVertex;

    public Pair<int, Task> CreateMesh(Buffer<Vertex> vertices, Buffer<uint> indices, MeshSurface[] surfaces);
    public Task? GetPendingMesh(int meshId);
    public bool IsMeshReady(int meshId);
    public IMesh? GetMesh(int meshId);
    public void FreeMeshes(params ReadOnlySpan<int> meshIds);

    public static IMeshFactory Get()
    {
        return Global.Provider.Get<IMeshFactory>();
    }
}