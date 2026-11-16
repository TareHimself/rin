using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Tests;

public class ImageShapeTests
{
    [Test]
    public void ALevelHoldsEveryLayerAtThatLevelsSize()
    {
        var cubemap = new ImageShape(new Extent2D(8u, 8u), 3, 6);

        Assert.Multiple(() =>
        {
            Assert.That(cubemap.LevelTexelCount(0), Is.EqualTo(8ul * 8 * 6));
            Assert.That(cubemap.LevelTexelCount(2), Is.EqualTo(2ul * 2 * 6));
        });
    }

    [Test]
    public void TheChainAddsUpItsLevels()
    {
        var cubemap = new ImageShape(new Extent2D(8u, 8u), 3, 6);

        Assert.That(cubemap.ChainTexelCount, Is.EqualTo((64ul + 16 + 4) * 6));
    }

    [Test]
    public void AnImageWithoutMipsOrLayersIsJustItsExtent()
    {
        Assert.That(new ImageShape(new Extent2D(5u, 3u)).ChainTexelCount, Is.EqualTo(15ul));
    }

    [Test]
    public void LevelsOfANonSquareImageStopShrinkingAtOne()
    {
        var strip = new ImageShape(new Extent2D(8u, 2u), 4);

        Assert.That(strip.ChainTexelCount, Is.EqualTo(16ul + 4 + 2 + 1));
    }
}
