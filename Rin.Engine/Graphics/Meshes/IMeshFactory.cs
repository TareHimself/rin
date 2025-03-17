using Rin.Engine.Core;
using Rin.Engine.Graphics.Descriptors;

namespace Rin.Engine.Graphics.Meshes;

public interface IMeshFactory : IDisposable
{
    public Pair<int,Task> CreateMesh(ReadOnlySpan<Vertex> vertices,ReadOnlySpan<uint> indices,MeshSurface[] surfaces);
    public Task? GetPendingMesh(int meshId);
    public bool IsMeshReady(int meshId);
    
    public void FreeMeshes(params int[] meshIds);
}