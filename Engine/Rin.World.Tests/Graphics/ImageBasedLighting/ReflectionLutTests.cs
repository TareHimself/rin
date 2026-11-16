using System.Numerics;
using Rin.Core.Graphics;
using Rin.Graphics.Null;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

public class ReflectionLutTests
{
    private const float Tolerance = 0.02f;

    [Test]
    public void ASmoothSurfaceSeenHeadOnMirrorsItsBaseReflectivity()
    {
        var response = ReflectionLut.Integrate(0.05f, 1f);

        Assert.Multiple(() =>
        {
            Assert.That(response.X, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(response.Y, Is.EqualTo(0f).Within(Tolerance));
        });
    }

    // Reference values from integrating the same model with 200,000 random samples.
    [TestCase(0.1f, 0.3f, 0.813f, 0.164f)]
    [TestCase(0.3f, 1.0f, 0.987f, 0.000f)]
    [TestCase(0.5f, 0.3f, 0.628f, 0.051f)]
    [TestCase(0.5f, 1.0f, 0.895f, 0.000f)]
    [TestCase(0.7f, 0.6f, 0.613f, 0.005f)]
    [TestCase(1.0f, 0.1f, 0.541f, 0.014f)]
    [TestCase(1.0f, 1.0f, 0.308f, 0.000f)]
    public void MatchesTheReferenceIntegral(float roughness, float facing, float scale, float bias)
    {
        var response = ReflectionLut.Integrate(roughness, facing);

        Assert.Multiple(() =>
        {
            Assert.That(response.X, Is.EqualTo(scale).Within(Tolerance));
            Assert.That(response.Y, Is.EqualTo(bias).Within(Tolerance));
        });
    }

    [Test]
    public void EverySurfaceGetsMoreMirrorLikeAtGrazingAngles()
    {
        Assert.That(ReflectionLut.Integrate(0.3f, 0.1f).Y, Is.GreaterThan(ReflectionLut.Integrate(0.3f, 0.9f).Y));
    }

    [Test]
    public void TheTableStaysWithinWhatASurfaceCanReflect()
    {
        var texels = new Vector2[ReflectionLut.Size * ReflectionLut.Size];

        ReflectionLut.Generate(texels);

        Assert.Multiple(() =>
        {
            foreach (var texel in texels)
            {
                Assert.That(texel.X, Is.InRange(0f, 1.001f));
                Assert.That(texel.Y, Is.InRange(0f, 1.001f));
                Assert.That(texel.X + texel.Y, Is.LessThanOrEqualTo(1.001f));
            }
        });
    }

    [Test]
    public void RowsAreRoughnessAndColumnsAreTheViewingAngle()
    {
        var texels = new Vector2[ReflectionLut.Size * ReflectionLut.Size];

        ReflectionLut.Generate(texels);

        var roughRow = ReflectionLut.Size - 1;
        var headOnColumn = ReflectionLut.Size - 1;
        var expected = ReflectionLut.Integrate((roughRow + 0.5f) / ReflectionLut.Size,
            (headOnColumn + 0.5f) / ReflectionLut.Size);
        Assert.That(texels[roughRow * ReflectionLut.Size + headOnColumn], Is.EqualTo(expected));
    }

    [Test]
    public void RejectsADestinationOfTheWrongSize()
    {
        Assert.Throws<ArgumentException>(() => ReflectionLut.Generate(new Vector2[3]));
    }

    [Test]
    public void TheTextureIsATwoChannelHalfFloatImage()
    {
        var graphics = new NullGraphicsModule();

        var texture = ReflectionLut.CreateTexture(graphics);

        Assert.Multiple(() =>
        {
            Assert.That(graphics.GetFormat(texture), Is.EqualTo(ImageFormat.RG16));
            Assert.That(graphics.GetExtent(texture), Is.EqualTo(new Extent2D(ReflectionLut.Size, ReflectionLut.Size)));
        });
    }

    [Test]
    public void EveryMapOfAStoreSharesOneTable()
    {
        var graphics = new NullGraphicsModule();
        var maps = new EnvironmentMaps();

        var first = maps.Get(maps.Create(TestEnvironments.Map(Vector3.One), graphics))!;
        var second = maps.Get(maps.Create(TestEnvironments.Map(Vector3.One), graphics))!;

        Assert.Multiple(() =>
        {
            Assert.That(first.ReflectionLut.Id, Is.Not.Zero);
            Assert.That(second.ReflectionLut, Is.EqualTo(first.ReflectionLut));
        });
    }
}
