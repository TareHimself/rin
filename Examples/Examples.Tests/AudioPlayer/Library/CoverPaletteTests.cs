using Examples.AudioPlayer.Library;

namespace Examples.Tests.AudioPlayer.Library;

public class CoverPaletteTests
{
    [Test]
    public void PicksTheLargestHueAsPrimaryAndTheNextDistinctOneAsSecondary()
    {
        var palette = CoverPalette.From(Pixels(64, 64, (x, _) => x < 40 ? (200, 30, 20) : (20, 40, 210)), 4);

        Assert.Multiple(() =>
        {
            Assert.That(palette.Primary.R, Is.GreaterThanOrEqualTo(0.8f));
            Assert.That(palette.Primary.B, Is.LessThan(0.3f));
            Assert.That(palette.Secondary.B, Is.GreaterThan(0.8f));
            Assert.That(palette.Secondary.R, Is.LessThan(0.3f));
        });
    }

    [Test]
    public void GrayArtGetsTheDefaultPalette()
    {
        var palette = CoverPalette.From(Pixels(16, 16, (x, y) => ((x + y) * 8 % 255, (x + y) * 8 % 255, (x + y) * 8 % 255)), 4);

        Assert.That(palette, Is.EqualTo(CoverPalette.Default));
    }

    [Test]
    public void ASingleHueGetsAShiftedSecondaryAndIsMadeVivid()
    {
        var palette = CoverPalette.From(Pixels(16, 16, (_, _) => (90, 50, 10)), 4);

        Assert.Multiple(() =>
        {
            Assert.That(palette.Primary.R, Is.GreaterThan(0.75f), "value is raised");
            Assert.That(palette.Primary.R - palette.Primary.B, Is.GreaterThan(0.4f), "saturation is kept");
            Assert.That(palette.Secondary, Is.Not.EqualTo(palette.Primary));
            Assert.That(palette.Secondary.G, Is.GreaterThan(palette.Primary.G), "hue shifted towards yellow");
        });
    }

    [Test]
    public void EmptyPixelsGetTheDefaultPalette()
    {
        Assert.That(CoverPalette.From([], 4), Is.EqualTo(CoverPalette.Default));
    }

    private static byte[] Pixels(int width, int height, Func<int, int, (int R, int G, int B)> color)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var (r, g, b) = color(x, y);
            var offset = (y * width + x) * 4;
            pixels[offset] = (byte)r;
            pixels[offset + 1] = (byte)g;
            pixels[offset + 2] = (byte)b;
            pixels[offset + 3] = 255;
        }

        return pixels;
    }
}
