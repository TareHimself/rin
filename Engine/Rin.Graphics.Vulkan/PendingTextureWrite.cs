using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan;

internal sealed class PendingTextureWrite : PendingWrite
{
    public PendingTextureWrite(PooledMemory<byte> data, in ImageRegion region, in ImageShape image,
        bool rebuildsMipChain = false) : base(data)
    {
        Offset = region.Offset;
        Extent = region.Extent;
        Level = region.Mip;
        Layer = region.Layer;
        RebuildsMipChain = rebuildsMipChain && image.MipLevels > 1;

        var fillsLevelZero = Level == 0 && Offset == default && Extent == image.Extent;
        var leavesNoOtherLevel = image.MipLevels == 1 || RebuildsMipChain;
        CoversWholeImage = image.LayerCount == 1 && fillsLevelZero && leavesNoOtherLevel;
    }

    private PendingTextureWrite(PooledMemory<byte> data) : base(data)
    {
        IsWholeChain = true;
        CoversWholeImage = true;
    }

    public Offset2D Offset { get; }
    public Extent2D Extent { get; }
    public uint Level { get; }
    public uint Layer { get; }

    /// <summary>
    ///     Whether the levels below level 0 of <see cref="Layer" /> are refilled from it after the write.
    /// </summary>
    public bool RebuildsMipChain { get; }

    /// <summary>
    ///     Whether the data holds every level of every layer, packed level by level.
    /// </summary>
    public bool IsWholeChain { get; }

    /// <summary>
    ///     Whether nothing of the old contents survives, so they can be discarded and earlier queued writes dropped.
    /// </summary>
    public bool CoversWholeImage { get; }

    public static PendingTextureWrite WholeChain(PooledMemory<byte> data)
    {
        return new PendingTextureWrite(data);
    }
}
