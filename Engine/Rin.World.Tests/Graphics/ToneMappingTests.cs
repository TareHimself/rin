using System.Numerics;
using Rin.World.Graphics;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Tests.Graphics;

public class ToneMappingTests
{
    private static readonly ToneMapCurve[] Curves = Enum.GetValues<ToneMapCurve>();

    private static float Smallest(Vector3 color) => float.Min(color.X, float.Min(color.Y, color.Z));

    private static float Largest(Vector3 color) => float.Max(color.X, float.Max(color.Y, color.Z));

    [TestCaseSource(nameof(Curves))]
    public void BlackStaysDark(ToneMapCurve curve)
    {
        Assert.That(Largest(ToneMapping.Apply(Vector3.Zero, curve)), Is.InRange(0f, 0.02f));
    }

    [TestCaseSource(nameof(Curves))]
    public void EveryBrightnessLandsInTheDisplayRange(ToneMapCurve curve)
    {
        Assert.Multiple(() =>
        {
            for (var brightness = 0.001f; brightness < 2000f; brightness *= 3f)
            {
                var mapped = ToneMapping.Apply(new Vector3(brightness, brightness * 0.5f, brightness * 0.1f), curve);
                Assert.That(Smallest(mapped), Is.GreaterThanOrEqualTo(0f), $"brightness {brightness}");
                Assert.That(Largest(mapped), Is.LessThanOrEqualTo(1f), $"brightness {brightness}");
            }
        });
    }

    [TestCaseSource(nameof(Curves))]
    public void ABrighterGreyNeverComesOutDarker(ToneMapCurve curve)
    {
        var previous = 0f;
        for (var brightness = 0.01f; brightness < 64f; brightness *= 1.3f)
        {
            var mapped = ToneMapping.Apply(new Vector3(brightness), curve).X;
            Assert.That(mapped, Is.GreaterThanOrEqualTo(previous - 1e-5f), $"brightness {brightness}");
            previous = mapped;
        }
    }

    [TestCaseSource(nameof(Curves))]
    public void AVeryBrightGreyIsNearlyWhite(ToneMapCurve curve)
    {
        Assert.That(Smallest(ToneMapping.Apply(new Vector3(1000f), curve)), Is.GreaterThan(0.9f));
    }

    [Test]
    public void ApplyPicksTheCurveItIsAskedFor()
    {
        var color = new Vector3(0.8f, 0.3f, 0.1f);

        Assert.Multiple(() =>
        {
            Assert.That(ToneMapping.Apply(color, ToneMapCurve.AgX), Is.EqualTo(ToneMapping.AgX(color)));
            Assert.That(ToneMapping.Apply(color, ToneMapCurve.Neutral), Is.EqualTo(ToneMapping.Neutral(color)));
            Assert.That(ToneMapping.Apply(color, ToneMapCurve.Aces), Is.EqualTo(ToneMapping.Aces(color)));
        });
    }

    [Test]
    public void NeutralKeepsMidColoursApartFromItsFixedOffset()
    {
        var color = new Vector3(0.3f, 0.4f, 0.5f);

        var mapped = ToneMapping.Neutral(color);

        Assert.That(Vector3.Distance(mapped, color - new Vector3(0.04f)), Is.LessThan(1e-5f));
    }

    [Test]
    public void NeutralFadesABrightColourTowardsWhiteWithoutReorderingItsChannels()
    {
        var mapped = ToneMapping.Neutral(new Vector3(40f, 10f, 2f));

        Assert.Multiple(() =>
        {
            Assert.That(mapped.X, Is.GreaterThan(mapped.Y));
            Assert.That(mapped.Y, Is.GreaterThan(mapped.Z));
            Assert.That(mapped.Z, Is.GreaterThan(0.5f));
        });
    }

    [Test]
    public void AgXFadesABrightSaturatedColourTowardsWhite()
    {
        var dim = ToneMapping.AgX(new Vector3(1f, 0f, 0f));
        var bright = ToneMapping.AgX(new Vector3(200f, 0f, 0f));

        Assert.That(Smallest(bright), Is.GreaterThan(Smallest(dim)));
    }
}
