using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

public class GgxSamplingTests
{
    [TestCase(0u, 0f)]
    [TestCase(1u, 0.5f)]
    [TestCase(2u, 0.25f)]
    [TestCase(3u, 0.75f)]
    [TestCase(4u, 0.125f)]
    public void TheRadicalInverseMirrorsTheBitsOfTheIndex(uint index, float expected)
    {
        Assert.That(GgxSampling.RadicalInverse(index), Is.EqualTo(expected));
    }

    [Test]
    public void HalfVectorsAreUnitLengthAndInTheUpperHemisphere()
    {
        Assert.Multiple(() =>
        {
            for (var i = 0u; i < 32u; i++)
            {
                var half = GgxSampling.HalfVector((i + 0.5f) / 32f, GgxSampling.RadicalInverse(i), 0.25f);
                Assert.That(half.Length(), Is.EqualTo(1f).Within(1e-5f));
                Assert.That(half.Z, Is.GreaterThanOrEqualTo(0f));
            }
        });
    }

    [Test]
    public void ASmootherSurfaceKeepsItsHalfVectorsCloserToTheNormal()
    {
        var smooth = GgxSampling.HalfVector(0.3f, 0.6f, 0.01f);
        var rough = GgxSampling.HalfVector(0.3f, 0.6f, 0.8f);

        Assert.That(smooth.Z, Is.GreaterThan(rough.Z));
    }

    [Test]
    public void TheDensityPeaksAtTheNormalAndRisesAsTheSurfaceSmooths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GgxSampling.Density(1f, 0.3f), Is.GreaterThan(GgxSampling.Density(0.7f, 0.3f)));
            Assert.That(GgxSampling.Density(1f, 0.1f), Is.GreaterThan(GgxSampling.Density(1f, 0.3f)));
            Assert.That(GgxSampling.Density(1f, 1f), Is.EqualTo(1f / MathF.PI).Within(1e-5f));
        });
    }
}
