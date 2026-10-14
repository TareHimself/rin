using Rin.Core.Graphics;
using Rin.Core.Shared.Video;

namespace Rin.Core.Tests.Shared.Video;

public class VideoFramePlanesTests
{
    private const uint Width = 1920;
    private const uint Height = 1080;
    private const ulong BytesPerLumaPixel = 1;
    private const ulong BytesPerChromaPair = 2;

    [Test]
    public void LumaIsOneBytePerPixel()
    {
        var planes = IVideoPlayer.PlanesOf(new Extent2D(Width, Height));

        Assert.That(planes.LumaByteSize, Is.EqualTo(Width * Height * BytesPerLumaPixel));
    }

    [Test]
    public void ChromaIsOnePairPerTwoByTwoPixels()
    {
        var planes = IVideoPlayer.PlanesOf(new Extent2D(Width, Height));

        Assert.That(planes.ChromaExtent, Is.EqualTo(new Extent2D(Width / 2, Height / 2)));
        Assert.That(planes.ChromaByteSize, Is.EqualTo(planes.ChromaExtent.Width * planes.ChromaExtent.Height * BytesPerChromaPair));
        Assert.That(planes.ByteSize, Is.EqualTo(planes.LumaByteSize + planes.ChromaByteSize));
    }

    [Test]
    public void OddSizesRoundTheChromaPlaneUp()
    {
        var even = IVideoPlayer.PlanesOf(new Extent2D(Width, Height));
        var odd = IVideoPlayer.PlanesOf(new Extent2D(Width - 1, Height - 1));

        Assert.That(odd.ChromaExtent, Is.EqualTo(even.ChromaExtent));
        Assert.That(odd.ChromaByteSize, Is.EqualTo(even.ChromaByteSize));
        Assert.That(odd.LumaByteSize, Is.EqualTo((Width - 1) * (Height - 1) * BytesPerLumaPixel));
    }
}
