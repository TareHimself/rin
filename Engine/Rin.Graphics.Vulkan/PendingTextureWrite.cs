using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan;

internal sealed class PendingTextureWrite(
    PooledMemory<byte> data,
    Offset2D offset,
    Extent2D extent,
    Extent2D imageExtent) : PendingWrite(data)
{
    public Offset2D Offset { get; } = offset;
    public Extent2D Extent { get; } = extent;
    public Extent2D ImageExtent { get; } = imageExtent;

    public bool CoversWholeImage => Offset == default && Extent == ImageExtent;
}
