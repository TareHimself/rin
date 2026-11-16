using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan.Tests;

public class PendingTextureWriteTests
{
    private static readonly Extent2D ImageExtent = new(4u, 4u);
    private static readonly ImageShape Plain = new(ImageExtent);
    private static readonly ImageShape Mipped = new(ImageExtent, 3);
    private static readonly ImageShape MippedCubemap = new(ImageExtent, 3, 6);

    private static PendingTextureWrite Write(in ImageRegion region, in ImageShape image, bool rebuildsMipChain = false)
    {
        var bytes = (int)(region.Extent.Width * region.Extent.Height * 4);
        return new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), region, image, rebuildsMipChain);
    }

    [Test]
    public void CarriesTheRegionItWritesTo()
    {
        var region = new ImageRegion(new Extent2D(1u, 2u), new Offset2D(1, 0), 1, 4);
        using var write = Write(region, MippedCubemap);

        Assert.Multiple(() =>
        {
            Assert.That(write.Extent, Is.EqualTo(region.Extent));
            Assert.That(write.Offset, Is.EqualTo(region.Offset));
            Assert.That(write.Level, Is.EqualTo(1u));
            Assert.That(write.Layer, Is.EqualTo(4u));
            Assert.That(write.IsWholeChain, Is.False);
        });
    }

    [Test]
    public void FillingAnImageWithoutMipsCoversIt()
    {
        using var write = Write(ImageRegion.Whole(ImageExtent), Plain);

        Assert.That(write.CoversWholeImage, Is.True);
    }

    [Test]
    public void AWriteToPartOfTheImageDoesNotCoverIt()
    {
        using var write = Write(ImageRegion.Whole(new Extent2D(2u, 2u)), Plain);

        Assert.That(write.CoversWholeImage, Is.False);
    }

    [Test]
    public void FillingLevelZeroLeavesTheLowerLevelsAlone()
    {
        using var write = Write(ImageRegion.Whole(ImageExtent), Mipped);

        Assert.Multiple(() =>
        {
            Assert.That(write.RebuildsMipChain, Is.False);
            Assert.That(write.CoversWholeImage, Is.False);
        });
    }

    [Test]
    public void FillingLevelZeroAndRebuildingTheChainCoversTheImage()
    {
        using var write = Write(ImageRegion.Whole(ImageExtent), Mipped, true);

        Assert.Multiple(() =>
        {
            Assert.That(write.RebuildsMipChain, Is.True);
            Assert.That(write.CoversWholeImage, Is.True);
        });
    }

    [Test]
    public void ThereIsNoChainToRebuildWithoutMips()
    {
        using var write = Write(ImageRegion.Whole(ImageExtent), Plain, true);

        Assert.That(write.RebuildsMipChain, Is.False);
    }

    [Test]
    public void AWriteToALowerLevelDoesNotCoverTheImage()
    {
        using var write = Write(ImageRegion.OfMip(ImageExtent, 1), Mipped);

        Assert.That(write.CoversWholeImage, Is.False);
    }

    [Test]
    public void AWriteToOneLayerLeavesTheOtherLayersAlone()
    {
        using var write = Write(ImageRegion.OfFace(ImageExtent, CubeFace.PositiveY), MippedCubemap, true);

        Assert.Multiple(() =>
        {
            Assert.That(write.RebuildsMipChain, Is.True);
            Assert.That(write.CoversWholeImage, Is.False);
        });
    }

    [Test]
    public void AWholeChainWriteCoversTheImage()
    {
        using var write = PendingTextureWrite.WholeChain(PooledMemory<byte>.CopyFrom(new byte[16]));

        Assert.Multiple(() =>
        {
            Assert.That(write.IsWholeChain, Is.True);
            Assert.That(write.CoversWholeImage, Is.True);
            Assert.That(write.RebuildsMipChain, Is.False);
        });
    }
}
