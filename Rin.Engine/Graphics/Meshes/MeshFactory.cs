using BepuPhysics.Collidables;
using Rin.Engine.Core;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics.Meshes;

public class MeshFactory : IMeshFactory
{
    private readonly object _sync = new();
    private bool _disposed = false;
    private readonly IdFactory _factory = new();
    private readonly Dictionary<int,DeviceMesh> _meshes = [];
    private readonly Dictionary<int,TaskCompletionSource> _pendingMeshes = [];
    
    public void Dispose()
    {
        _disposed = true;
        lock (_sync)
        {
            foreach (var (key, pending) in _pendingMeshes)
            {
                pending.SetCanceled();
            }
            
            foreach (var (key, mesh) in _meshes)
            {
                mesh.Dispose();
            }
            
            _pendingMeshes.Clear();
            _meshes.Clear();
        }
    }

    private async Task AsyncCreateMesh(int id,NativeBuffer<Vertex> vertices, NativeBuffer<uint> indices,MeshSurface[] surfaces)
    {
        using (vertices)
        using(indices)
        {
            if(_disposed) return;
            
            var verticesByteSize = (ulong)vertices.GetByteSize();
            var indicesByteSize = (ulong)indices.GetByteSize();
            var vertexBuffer = SGraphicsModule.Get().GetAllocator().NewBuffer(verticesByteSize,
                VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT |
                VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT |
                VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT,
                VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, debugName: "Vertex Buffer");

            var indexBuffer = SGraphicsModule.Get().GetAllocator().NewBuffer(indicesByteSize,
                VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT |
                VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT,
                VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, debugName: "Vertex Index Buffer");

            using var stagingBuffer = SGraphicsModule.Get().NewTransferBuffer(verticesByteSize + indicesByteSize);

            stagingBuffer.Write(vertices);
            stagingBuffer.Write(indices, verticesByteSize);

            await SGraphicsModule.Get().TransferSubmit(cmd =>
            {
                var vertexCopy = new VkBufferCopy
                {
                    size = verticesByteSize,
                    dstOffset = 0,
                    srcOffset = 0
                };


                var indicesCopy = new VkBufferCopy
                {
                    size = indicesByteSize,
                    srcOffset = verticesByteSize,
                    dstOffset = 0
                };

                unsafe
                {
                    vkCmdCopyBuffer(cmd, stagingBuffer.NativeBuffer, vertexBuffer.NativeBuffer, 1, &vertexCopy);
                    vkCmdCopyBuffer(cmd, stagingBuffer.NativeBuffer, indexBuffer.NativeBuffer, 1, &indicesCopy);
                }
            });

            var mesh = new DeviceMesh(vertexBuffer, indexBuffer, surfaces);

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
    
    public int CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices,MeshSurface[] surfaces)
    {
        var id = _factory.NewId();
        var nVertices = new NativeBuffer<Vertex>(vertices);
        var nIndices = new NativeBuffer<uint>(indices);
        
        lock (_sync)
        {
            _pendingMeshes[id] = new TaskCompletionSource();
        }
        
        Task.Run(() => AsyncCreateMesh(id,nVertices, nIndices, surfaces)).ConfigureAwait(false);
        
        return id;
    }

    public bool IsMeshReady(int meshId)
    {
        lock (_sync)
        {
            return _meshes.ContainsKey(meshId);
        }
    }

    public Task GetCreateTask(int meshId)
    {
        TaskCompletionSource? toComplete;
        lock (_sync)
        {
            _pendingMeshes.TryGetValue(meshId, out toComplete);
        }

        
        return toComplete?.Task ?? Task.CompletedTask;
    }

    public void FreeMeshes(params int[] meshIds)
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
}