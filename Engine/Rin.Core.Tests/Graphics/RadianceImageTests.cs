using System.Text;
using Rin.Core.Graphics;

namespace Rin.Core.Tests.Graphics;

public class RadianceImageTests
{
    private const int RunLengthWidth = 8;

    private static byte[] Hdr(int width, int height, params byte[] pixels)
    {
        return Hdr($"-Y {height} +X {width}", pixels);
    }

    private static byte[] Hdr(string resolution, params byte[] pixels)
    {
        var header = Encoding.ASCII.GetBytes($"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n{resolution}\n");
        return [.. header, .. pixels];
    }

    private static float[] Decode(byte[] file)
    {
        var extent = RadianceImage.ReadExtent(file);
        var rgba = new float[extent.Width * extent.Height * 4];
        RadianceImage.Decode(file, rgba);
        return rgba;
    }

    [Test]
    public void RecognisesBothMagicLines()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RadianceImage.IsRadiance("#?RADIANCE\n"u8), Is.True);
            Assert.That(RadianceImage.IsRadiance("#?RGBE\n"u8), Is.True);
            Assert.That(RadianceImage.IsRadiance("\u0089PNG"u8), Is.False);
        });
    }

    [Test]
    public void ReadsTheExtentFromTheResolutionLine()
    {
        Assert.That(RadianceImage.ReadExtent(Hdr(3, 2)), Is.EqualTo(new Extent2D(3u, 2u)));
    }

    [Test]
    public void AcceptsWindowsLineEndings()
    {
        var file = "#?RADIANCE\r\nFORMAT=32-bit_rle_rgbe\r\n\r\n-Y 2 +X 3\r\n"u8.ToArray();

        Assert.That(RadianceImage.ReadExtent(file), Is.EqualTo(new Extent2D(3u, 2u)));
    }

    [Test]
    public void ScalesMantissasByTheSharedExponent()
    {
        // Exponent 128 scales by 2^-8 and 129 by 2^-7.
        var rgba = Decode(Hdr(2, 1, 128, 64, 0, 128, 128, 64, 32, 129));

        Assert.That(rgba, Is.EqualTo(new[] { 0.5f, 0.25f, 0f, 1f, 1f, 0.5f, 0.25f, 1f }));
    }

    [Test]
    public void DecodesValuesAboveOne()
    {
        var rgba = Decode(Hdr(1, 1, 128, 128, 128, 136));

        Assert.That(rgba, Is.EqualTo(new[] { 128f, 128f, 128f, 1f }));
    }

    [Test]
    public void AZeroExponentIsBlack()
    {
        var rgba = Decode(Hdr(1, 1, 200, 100, 50, 0));

        Assert.That(rgba, Is.EqualTo(new[] { 0f, 0f, 0f, 1f }));
    }

    [Test]
    public void DecodesRunsAndLiteralsInARunLengthScanline()
    {
        byte[] scanline =
        [
            2, 2, 0, RunLengthWidth,
            128 + 8, 128,
            8, 0, 32, 64, 96, 128, 160, 192, 224,
            128 + 3, 0, 5, 128, 128, 128, 128, 128,
            128 + 8, 128
        ];

        var rgba = Decode(Hdr(RunLengthWidth, 1, scanline));

        Assert.Multiple(() =>
        {
            for (var x = 0; x < RunLengthWidth; x++)
            {
                var expected = new[] { 0.5f, x * 32 / 256f, x < 3 ? 0f : 0.5f, 1f };
                Assert.That(rgba.AsSpan(x * 4, 4).ToArray(), Is.EqualTo(expected), $"pixel {x}");
            }
        });
    }

    [Test]
    public void DecodesEachRunLengthScanlineIntoItsOwnRow()
    {
        byte[] scanlines =
        [
            2, 2, 0, RunLengthWidth, 128 + 8, 128, 128 + 8, 0, 128 + 8, 0, 128 + 8, 128,
            2, 2, 0, RunLengthWidth, 128 + 8, 0, 128 + 8, 128, 128 + 8, 0, 128 + 8, 128
        ];

        var rgba = Decode(Hdr(RunLengthWidth, 2, scanlines));

        Assert.Multiple(() =>
        {
            Assert.That(rgba.AsSpan(0, 4).ToArray(), Is.EqualTo(new[] { 0.5f, 0f, 0f, 1f }));
            Assert.That(rgba.AsSpan(RunLengthWidth * 4, 4).ToArray(), Is.EqualTo(new[] { 0f, 0.5f, 0f, 1f }));
        });
    }

    [Test]
    public void RejectsADestinationOfTheWrongSize()
    {
        var file = Hdr(1, 1, 128, 128, 128, 128);

        Assert.Throws<ArgumentException>(() => RadianceImage.Decode(file, new float[3]));
    }

    [Test]
    public void RejectsOtherFileTypes()
    {
        Assert.Throws<InvalidDataException>(() => RadianceImage.ReadExtent("\u0089PNG\n\n"u8));
    }

    [Test]
    public void RejectsAMissingPixelFormat()
    {
        var file = "#?RADIANCE\n\n-Y 1 +X 1\n"u8.ToArray();

        Assert.Throws<InvalidDataException>(() => RadianceImage.ReadExtent(file));
    }

    [TestCase("+Y 1 +X 1")]
    [TestCase("-Y 1 -X 1")]
    [TestCase("+X 1 -Y 1")]
    [TestCase("-Y 0 +X 1")]
    public void RejectsUnsupportedResolutionLines(string resolution)
    {
        Assert.Throws<InvalidDataException>(() => RadianceImage.ReadExtent(Hdr(resolution)));
    }

    [Test]
    public void RejectsTruncatedFlatPixels()
    {
        var file = Hdr(2, 1, 128, 128, 128, 128);

        Assert.Throws<InvalidDataException>(() => Decode(file));
    }

    [Test]
    public void RejectsATruncatedRunLengthScanline()
    {
        var file = Hdr(RunLengthWidth, 1, 2, 2, 0, RunLengthWidth, 128 + 8, 128, 4, 1, 2);

        Assert.Throws<InvalidDataException>(() => Decode(file));
    }

    [Test]
    public void RejectsARunLongerThanTheScanline()
    {
        var file = Hdr(RunLengthWidth, 1, 2, 2, 0, RunLengthWidth, 128 + 9, 128);

        Assert.Throws<InvalidDataException>(() => Decode(file));
    }
}
