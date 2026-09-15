using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Core.Shared.Buffers;

namespace Rin.World.Graphics.Mesh;

public class MeshFactory : IMeshFactory
{
    private readonly IdFactory _factory = new();
    private readonly Dictionary<int, DeviceMesh> _meshes = [];
    private readonly Dictionary<int, TaskCompletionSource> _pendingMeshes = [];
    private readonly object _sync = new();
    private bool _disposed;

    public void Dispose()
    {
        _disposed = true;
        lock (_sync)
        {
            foreach (var (key, pending) in _pendingMeshes) pending.SetCanceled();

            foreach (var (key, mesh) in _meshes) mesh.Dispose();

            _pendingMeshes.Clear();
            _meshes.Clear();
        }
    }

    public Pair<int, Task> CreateMesh<TVertexFormat>(Buffer<TVertexFormat> vertices, Buffer<uint> indices,
        MeshSurface[] surfaces) where TVertexFormat : unmanaged, IVertex
    {
        var id = _factory.NewId();
        var nVertices = vertices.Copy();
        var nIndices = indices.Copy();

        Task task;
        lock (_sync)
        {
            _pendingMeshes[id] = new TaskCompletionSource();
            task = _pendingMeshes[id].Task;
        }

        Task.Run(() => CreateMeshInternal(id, nVertices, nIndices, surfaces));

        return new Pair<int, Task>(id, task);
    }

    public Pair<int, Task> CreateMesh(Buffer<Vertex> vertices, Buffer<uint> indices, MeshSurface[] surfaces)
    {
        return CreateMesh<Vertex>(vertices, indices, surfaces);
    }

    public bool IsMeshReady(int meshId)
    {
        lock (_sync)
        {
            return _meshes.ContainsKey(meshId);
        }
    }

    public IMesh? GetMesh(int meshId)
    {
        lock (_sync)
        {
            if (_meshes.TryGetValue(meshId, out var mesh)) return mesh;
        }

        return null;
    }

    public Task? GetPendingMesh(int meshId)
    {
        lock (_sync)
        {
            if (_pendingMeshes.TryGetValue(meshId, out var src)) return src.Task;
        }

        return null;
    }

    public void FreeMeshes(params ReadOnlySpan<int> meshIds)
    {
        lock (_sync)
        {
            foreach (var meshId in meshIds)
            {
                if (!_meshes.TryGetValue(meshId, out var mesh)) continue;
                mesh.Dispose();
                _meshes.Remove(meshId);
            }
        }
    }

    private void CreateMeshInternal<TVertexFormat>(int id, Buffer<TVertexFormat> vertices, Buffer<uint> indices,
        MeshSurface[] surfaces) where TVertexFormat : unmanaged, IVertex
    {
        using (vertices)
        using (indices)
        {
            if (_disposed) return;

            var graphics = IGraphicsModule.Get();
            var verticesByteSize = vertices.GetByteSize();
            var indicesByteSize = indices.GetByteSize();

            var vertexBuffer = graphics.NewStorageBuffer(verticesByteSize, false);
            var indexBuffer = graphics.NewStorageBuffer(indicesByteSize, false);

            vertexBuffer.Write(vertices);
            indexBuffer.Write(indices);

            var mesh = new DeviceMesh(vertexBuffer, indexBuffer, surfaces, Utils.ByteSizeOf<TVertexFormat>());

            TaskCompletionSource? toComplete;
            lock (_sync)
            {
                _meshes[id] = mesh;
                _pendingMeshes.TryGetValue(id, out toComplete);
                _pendingMeshes.Remove(id);
            }

            toComplete?.SetResult();
        }
    }
}
