namespace Rin.Framework.Graphics.Meshes;

public interface IMeshFactory : IDisposable
{
    public Pair<int, Task> CreateMesh<TVertexFormat>(Buffer<TVertexFormat> vertices, Buffer<uint> indices,
        MeshSurface[] surfaces) where TVertexFormat : unmanaged, IVertex;

    public Pair<int, Task> CreateMesh(Buffer<Vertex> vertices, Buffer<uint> indices, MeshSurface[] surfaces);
    public Task? GetPendingMesh(int meshId);
    public bool IsMeshReady(int meshId);
    public IMesh? GetMesh(int meshId);
    public void FreeMeshes(params int[] meshIds);
}