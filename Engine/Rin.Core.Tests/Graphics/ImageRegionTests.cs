using Rin.Core.Graphics;

namespace Rin.Core.Tests.Graphics;

public class ImageRegionTests
{
    private static readonly Extent2D Extent = new(8u, 4u);

    [Test]
    public void WholeIsAllOfLevelZeroOfTheFirstLayer()
    {
        Assert.That(ImageRegion.Whole(Extent), Is.EqualTo(new ImageRegion(Extent, default, 0, 0)));
    }

    [Test]
    public void RectKeepsItsOffset()
    {
        var region = ImageRegion.Rect(new Offset2D(2, 1), new Extent2D(3u, 2u));

        Assert.That(region, Is.EqualTo(new ImageRegion(new Extent2D(3u, 2u), new Offset2D(2, 1))));
    }

    [Test]
    public void OfMipSizesTheRegionToThatLevel()
    {
        var region = ImageRegion.OfMip(Extent, 2, 3);

        Assert.That(region, Is.EqualTo(new ImageRegion(new Extent2D(2u, 1u), default, 2, 3)));
    }

    [Test]
    public void OfFaceUsesTheFaceAsTheLayer()
    {
        var region = ImageRegion.OfFace(new Extent2D(8u, 8u), CubeFace.NegativeY, 1);

        Assert.That(region, Is.EqualTo(new ImageRegion(new Extent2D(4u, 4u), default, 1, 3)));
    }

    [Test]
    public void FacesAreInGpuLayerOrder()
    {
        CubeFace[] expected =
        [
            CubeFace.PositiveX, CubeFace.NegativeX, CubeFace.PositiveY, CubeFace.NegativeY, CubeFace.PositiveZ,
            CubeFace.NegativeZ
        ];

        Assert.That(Enum.GetValues<CubeFace>(), Is.EqualTo(expected));
    }
}
