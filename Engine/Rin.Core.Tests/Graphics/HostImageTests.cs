using System.Runtime.InteropServices;
using Rin.Core.Graphics;

namespace Rin.Core.Tests.Graphics;

public class HostImageTests
{
    [Test]
    public void DecodesRadianceFilesAsFloatRgba()
    {
        byte[] file =
        [
            .. "#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 1 +X 2\n"u8,
            128, 64, 0, 128,
            128, 64, 32, 129
        ];

        using var image = HostImage.Create(file);
        using var raw = new MemoryStream();
        image.SaveRaw(raw);
        var pixels = MemoryMarshal.Cast<byte, float>(raw.ToArray()).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(image.Extent, Is.EqualTo(new Extent2D(2u, 1u)));
            Assert.That(image.Format, Is.EqualTo(HostImageFormat.RGBA32));
            Assert.That(pixels, Is.EqualTo(new[] { 0.5f, 0.25f, 0f, 1f, 1f, 0.5f, 0.25f, 1f }));
        });
    }
}
