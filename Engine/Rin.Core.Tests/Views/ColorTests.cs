using Rin.Core.Views;

namespace Rin.Core.Tests.Views;

public class ColorTests
{
    [Test]
    public void FromRgb_is_opaque()
    {
        var color = Color.FromRgb(0.2f, 0.4f, 0.6f);

        Assert.That((color.R, color.G, color.B, color.A), Is.EqualTo((0.2f, 0.4f, 0.6f, 1f)));
    }

    [Test]
    public void FromBytes_divides_by_255()
    {
        var color = Color.FromBytes(255, 0, 51, 128);

        Assert.Multiple(() =>
        {
            Assert.That(color.R, Is.EqualTo(1f));
            Assert.That(color.G, Is.EqualTo(0f));
            Assert.That(color.B, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(color.A, Is.EqualTo(0.502f).Within(0.001f));
        });
    }

    [TestCase(0f, 1f, 0f, 0f)]
    [TestCase(1f / 3f, 0f, 1f, 0f)]
    [TestCase(2f / 3f, 0f, 0f, 1f)]
    [TestCase(1f, 1f, 0f, 0f)]
    [TestCase(1.5f, 0f, 1f, 1f)]
    public void FromHsv_wraps_hue_around_the_wheel(float hue, float r, float g, float b)
    {
        var color = Color.FromHsv(hue, 1f, 1f);

        Assert.Multiple(() =>
        {
            Assert.That(color.R, Is.EqualTo(r).Within(0.0001f));
            Assert.That(color.G, Is.EqualTo(g).Within(0.0001f));
            Assert.That(color.B, Is.EqualTo(b).Within(0.0001f));
            Assert.That(color.A, Is.EqualTo(1f));
        });
    }

    [Test]
    public void FromHsv_with_zero_saturation_is_grey_and_keeps_alpha()
    {
        var color = Color.FromHsv(0.7f, 0f, 0.3f, 0.5f);

        Assert.That((color.R, color.G, color.B, color.A), Is.EqualTo((0.3f, 0.3f, 0.3f, 0.5f)));
    }

    [Test]
    public void FromHtml_matches_FromBytes()
    {
        var html = Color.FromHtml("#123456");
        var bytes = Color.FromBytes(18, 52, 86);

        Assert.That((html.R, html.G, html.B, html.A), Is.EqualTo((bytes.R, bytes.G, bytes.B, bytes.A)));
    }
}
