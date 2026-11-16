using System.Numerics;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

public class PanoramaTests
{
    private static void AssertClose(Vector3 actual, Vector3 expected, float tolerance = 1e-5f)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(tolerance), $"{actual} vs {expected}");
    }

    [Test]
    public void ForwardIsTheCentreAndRightIsThreeQuartersAcross()
    {
        Assert.Multiple(() =>
        {
            AssertClose(Panorama.Direction(0.5f, 0.5f), Vector3.UnitZ);
            AssertClose(Panorama.Direction(0.75f, 0.5f), Vector3.UnitX);
            AssertClose(Panorama.Direction(0.25f, 0.5f), -Vector3.UnitX);
            AssertClose(Panorama.Direction(0f, 0.5f), -Vector3.UnitZ);
        });
    }

    [Test]
    public void UpIsTheTopRowAndDownIsTheBottomRow()
    {
        Assert.Multiple(() =>
        {
            AssertClose(Panorama.Direction(0.3f, 0f), Vector3.UnitY);
            AssertClose(Panorama.Direction(0.3f, 1f), -Vector3.UnitY);
        });
    }

    [TestCase(100, 100, 1)]
    [TestCase(101, 100, 2)]
    [TestCase(4096, 1024, 4)]
    [TestCase(7386, 4096, 2)]
    [TestCase(50, 100, 1)]
    public void TheReductionFactorIsTheSmallestThatFits(int width, int maxWidth, int expected)
    {
        Assert.That(Panorama.ReductionFactor(width, maxWidth), Is.EqualTo(expected));
    }

    [Test]
    public void ReduceAveragesEachBlock()
    {
        Vector4[] source =
        [
            new(1f), new(3f), new(10f), new(10f),
            new(5f), new(7f), new(10f), new(30f)
        ];
        var reduced = new Vector4[2];

        Panorama.Reduce(source, 4, 2, 2, reduced);

        Assert.That(reduced, Is.EqualTo(new[] { new Vector4(4f), new Vector4(15f) }));
    }

    [Test]
    public void ReduceDropsWhatDoesNotFillABlock()
    {
        var source = new Vector4[5 * 3];
        Array.Fill(source, new Vector4(2f));
        var reduced = new Vector4[2];

        Panorama.Reduce(source, 5, 3, 2, reduced);

        Assert.That(reduced, Is.All.EqualTo(new Vector4(2f)));
    }

    [Test]
    public void ReduceRejectsADestinationOfTheWrongSize()
    {
        Assert.Throws<ArgumentException>(() => Panorama.Reduce(new Vector4[8], 4, 2, 2, new Vector4[3]));
    }

    [Test]
    public void AConstantPanoramaGivesIrradianceOfItsColourTimesPi()
    {
        var texels = new Vector4[64 * 32];
        Array.Fill(texels, new Vector4(1f, 2f, 3f, 1f));

        var irradiance = Panorama.ProjectIrradiance(texels, 64, 32);

        AssertClose(irradiance.Evaluate(Vector3.UnitX), new Vector3(1f, 2f, 3f) * MathF.PI, 0.02f);
    }

    [Test]
    public void ALitUpperHalfLightsSurfacesFacingUpAndNotDown()
    {
        var texels = new Vector4[64 * 32];
        texels.AsSpan(0, 64 * 16).Fill(Vector4.One);

        var irradiance = Panorama.ProjectIrradiance(texels, 64, 32);

        Assert.Multiple(() =>
        {
            AssertClose(irradiance.Evaluate(Vector3.UnitY), new Vector3(MathF.PI), 0.05f);
            AssertClose(irradiance.Evaluate(Vector3.UnitZ), new Vector3(MathF.PI / 2f), 0.05f);
            AssertClose(irradiance.Evaluate(-Vector3.UnitY), Vector3.Zero, 0.05f);
        });
    }
}
