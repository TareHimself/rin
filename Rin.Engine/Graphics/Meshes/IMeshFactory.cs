using Rin.Engine.Graphics.Descriptors;

namespace Rin.Engine.Graphics.Meshes;

public interface IMeshFactory : IDisposable
{
    public int CreateMesh(ReadOnlySpan<Vertex> vertices,ReadOnlySpan<uint> indices,MeshSurface[] surfaces);
    public bool IsMeshReady(int meshId);
    public Task GetCreateTask(int meshId);
    public void FreeMeshes(params int[] meshIds);
}