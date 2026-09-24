using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan;

internal sealed class PendingBufferWrite(PooledMemory<byte> data, ulong offset) : PendingWrite(data)
{
    public ulong Offset { get; } = offset;
    public ulong End => Offset + (ulong)Data.Count;

    public bool Overlaps(PendingBufferWrite other)
    {
        return Offset < other.End && other.Offset < End;
    }
}
