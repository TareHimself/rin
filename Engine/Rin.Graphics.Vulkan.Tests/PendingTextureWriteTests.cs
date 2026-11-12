using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan.Tests;

public class PendingTextureWriteTests
{
    private static readonly Extent2D ImageExtent = new(4u, 4u);

    [Test]
    public void AWriteWithoutALevelTargetsLevelZeroAndRebuildsTheChain()
    {
        using var write = Write(ImageExtent, ImageExtent);

        Assert.Multiple(() =>
        {
            Assert.That(write.Level, Is.EqualTo(0u));
            Assert.That(write.RebuildsMipChain, Is.True);
            Assert.That(write.CoversWholeImage, Is.True);
        });
    }

    [Test]
    public void AWriteToPartOfTheImageDoesNotCoverIt()
    {
        using var write = Write(new Extent2D(2u, 2u), ImageExtent);

        Assert.That(write.CoversWholeImage, Is.False);
    }

    [Test]
    public void AWriteToAnExplicitLevelLeavesTheOtherLevelsAlone()
    {
        var levelExtent = ImageExtent.MipExtent(1);
        using var write = Write(levelExtent, levelExtent, 1);

        Assert.Multiple(() =>
        {
            Assert.That(write.Level, Is.EqualTo(1u));
            Assert.That(write.RebuildsMipChain, Is.False);
            Assert.That(write.CoversWholeImage, Is.False);
        });
    }

    [Test]
    public void AnExplicitWriteToAllOfLevelZeroDoesNotDiscardTheLowerLevels()
    {
        using var write = Write(ImageExtent, ImageExtent, 0);

        Assert.That(write.CoversWholeImage, Is.False);
    }

    private static PendingTextureWrite Write(Extent2D extent, Extent2D levelExtent, int mipLevel = -1)
    {
        var bytes = (int)(extent.Width * extent.Height * 4);
        return new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), default, extent, levelExtent,
            mipLevel);
    }
}
