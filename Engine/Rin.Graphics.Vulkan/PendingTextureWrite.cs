using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan;

internal sealed class PendingTextureWrite(
    PooledMemory<byte> data,
    Offset2D offset,
    Extent2D extent,
    Extent2D imageExtent,
    int mipLevel = -1) : PendingWrite(data)
{
    public Offset2D Offset { get; } = offset;
    public Extent2D Extent { get; } = extent;
    public Extent2D ImageExtent { get; } = imageExtent;

    /// <summary>
    ///     The level written, where a negative <c>mipLevel</c> means level 0.
    /// </summary>
    public uint Level { get; } = (uint)int.Max(mipLevel, 0);

    public bool RebuildsMipChain { get; } = mipLevel < 0;

    // A write to one explicit level leaves the other levels' contents in place, so it never covers the image.
    public bool CoversWholeImage => RebuildsMipChain && Offset == default && Extent == ImageExtent;
}
