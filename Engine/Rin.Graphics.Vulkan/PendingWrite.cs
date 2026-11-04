using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan;

internal abstract class PendingWrite(PooledMemory<byte> data) : PendingResourceAction
{
    private const ulong StagingAlignment = 16;

    public PooledMemory<byte> Data { get; } = data;

    public ulong StagingSize => ((ulong)Data.Count + StagingAlignment - 1) & ~(StagingAlignment - 1);

    protected override void Release()
    {
        Data.Dispose();
    }
}
