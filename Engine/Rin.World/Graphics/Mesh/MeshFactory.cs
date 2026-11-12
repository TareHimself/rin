using System.Runtime.InteropServices;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.World.Graphics.Mesh;

public class MeshFactory : IMeshFactory
{
    private readonly List<Slot> _slots = [];
    private readonly Stack<int> _freeSlots = [];
    private readonly Lock _sync = new();
    private bool _disposed;

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            for (var i = 0; i < _slots.Count; i++) FreeSlot(i);
        }
    }

    public MeshHandle CreateMesh<TVertexFormat>(ReadOnlySpan<TVertexFormat> vertices, ReadOnlySpan<uint> indices,
        MeshSurface[] surfaces) where TVertexFormat : unmanaged, IVertex
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var graphics = IGraphicsModule.Get();
        var vertexBuffer = CreateAndUpload(graphics, vertices, BufferCreateFlags.Storage | BufferCreateFlags.TransferDst);
        var indexBuffer = CreateAndUpload(graphics, indices,
            BufferCreateFlags.Storage | BufferCreateFlags.TransferDst | BufferCreateFlags.Index);
        var mesh = new DeviceMesh(vertexBuffer, indexBuffer, surfaces, Utils.ByteSizeOf<TVertexFormat>());

        lock (_sync)
        {
            return Register(mesh);
        }
    }

    public IMesh? GetMesh(MeshHandle mesh)
    {
        lock (_sync)
        {
            return IsLive(mesh) ? _slots[(int)mesh.Index].Mesh : null;
        }
    }

    public void FreeMeshes(params ReadOnlySpan<MeshHandle> meshes)
    {
        lock (_sync)
        {
            foreach (var mesh in meshes)
                if (IsLive(mesh))
                    FreeSlot((int)mesh.Index);
        }
    }

    private MeshHandle Register(DeviceMesh mesh)
    {
        if (!_freeSlots.TryPop(out var index))
        {
            index = _slots.Count;
            _slots.Add(default);
        }

        var generation = _slots[index].Generation + 1;
        _slots[index] = new Slot(mesh, generation);
        return new MeshHandle((uint)index, generation);
    }

    // Bumping the generation makes every handle to the freed mesh stale.
    private void FreeSlot(int index)
    {
        var slot = _slots[index];
        if (slot.Mesh is null) return;

        slot.Mesh.Dispose();
        _slots[index] = new Slot(null, slot.Generation + 1);
        _freeSlots.Push(index);
    }

    private bool IsLive(MeshHandle mesh) =>
        mesh.IsValid && mesh.Index < _slots.Count && _slots[(int)mesh.Index].Generation == mesh.Generation;

    private static DeviceBufferView CreateAndUpload<T>(IGraphicsModule graphics, ReadOnlySpan<T> data,
        BufferCreateFlags flags) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(data);
        var handle = graphics.CreateBuffer((ulong)bytes.Length, flags);
        using var pooled = PooledMemory<byte>.CopyFrom(bytes);
        graphics.QueueBufferUpload(handle, pooled.AsMemory());
        return new DeviceBufferView(handle, 0, (ulong)bytes.Length);
    }

    private readonly record struct Slot(DeviceMesh? Mesh, uint Generation);
}
