using Rin.Core;
using Rin.Core.Shared.Buffers;

namespace Rin.World.Graphics.Mesh;

public interface IMeshFactory : IDisposable, IProviderResolvable<IMeshFactory>
{
    public Task CreateMesh<TVertexFormat>(out int meshId, Buffer<TVertexFormat> vertices, Buffer<uint> indices,
        MeshSurface[] surfaces) where TVertexFormat : unmanaged, IVertex;

    public Task CreateMesh(out int meshId, Buffer<Vertex> vertices, Buffer<uint> indices, MeshSurface[] surfaces);
    public Task? GetPendingMesh(int meshId);
    public bool IsMeshReady(int meshId);
    public IMesh? GetMesh(int meshId);
    public void FreeMeshes(params ReadOnlySpan<int> meshIds);
}