using Rin.Core.Graphics;

namespace Rin.Core.Tests.Graphics;

public class Extent2DTests
{
    [TestCase(1u, 1u, 1u)]
    [TestCase(2u, 2u, 2u)]
    [TestCase(5u, 5u, 3u)]
    [TestCase(1024u, 512u, 11u)]
    [TestCase(1u, 256u, 9u)]
    public void FullMipLevelsCountsHalvingsOfTheLongerSideDownToOne(uint width, uint height, uint expected)
    {
        Assert.That(new Extent2D(width, height).FullMipLevels, Is.EqualTo(expected));
    }

    [Test]
    public void AnEmptyExtentStillHasOneLevel()
    {
        Assert.That(Extent2D.Zero.FullMipLevels, Is.EqualTo(1u));
    }

    [Test]
    public void MipExtentHalvesEachLevelAndRoundsDown()
    {
        var extent = new Extent2D(5u, 12u);

        Assert.Multiple(() =>
        {
            Assert.That(extent.MipExtent(0), Is.EqualTo(extent));
            Assert.That(extent.MipExtent(1), Is.EqualTo(new Extent2D(2u, 6u)));
            Assert.That(extent.MipExtent(2), Is.EqualTo(new Extent2D(1u, 3u)));
        });
    }

    [Test]
    public void MipExtentNeverDropsBelowOne()
    {
        var extent = new Extent2D(1024u, 4u);

        Assert.Multiple(() =>
        {
            Assert.That(extent.MipExtent(3), Is.EqualTo(new Extent2D(128u, 1u)));
            Assert.That(extent.MipExtent(extent.FullMipLevels - 1), Is.EqualTo(new Extent2D(1u, 1u)));
            Assert.That(extent.MipExtent(40), Is.EqualTo(new Extent2D(1u, 1u)));
        });
    }
}
