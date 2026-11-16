using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

public class EnvironmentMapTests
{
    private const int FaceSize = 16;
    private static readonly Vector3 Sky = new(0.25f, 0.5f, 1f);

    private static EnvironmentMap Build(Func<Vector3, Vector3> colourOf, int faceSize = FaceSize)
    {
        return EnvironmentMap.FromEquirect(TestEquirect.Create(colourOf), TestEquirect.Extent, faceSize);
    }

    [TestCase(16, 2)]
    [TestCase(256, 6)]
    [TestCase(512, 7)]
    public void MipsRunFromTheFaceSizeDownToEightTexels(int faceSize, int expectedMips)
    {
        var map = Build(_ => Sky, faceSize);

        Assert.Multiple(() =>
        {
            Assert.That(map.FaceSize, Is.EqualTo(faceSize));
            Assert.That(map.MipCount, Is.EqualTo(expectedMips));
            Assert.That(faceSize >> (map.MipCount - 1), Is.EqualTo(8));
        });
    }

    [Test]
    public void RoughnessRunsFromSharpAtMipZeroToFullyRoughAtTheLast()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EnvironmentMap.MipRoughness(0, 6), Is.EqualTo(0f));
            Assert.That(EnvironmentMap.MipRoughness(5, 6), Is.EqualTo(1f));
        });
    }

    [Test]
    public void APanoramaNoSharperThanTheCubemapIsKeptAsItIs()
    {
        var map = Build(_ => Sky, 512);

        Assert.Multiple(() =>
        {
            Assert.That(map.PanoramaExtent, Is.EqualTo(TestEquirect.Extent));
            Assert.That(map.Panorama.Length,
                Is.EqualTo(TestEquirect.Extent.Width * TestEquirect.Extent.Height * EnvironmentMap.Channels));
        });
    }

    [Test]
    public void APanoramaSharperThanTheCubemapIsShrunkToTwoTexelsPerFaceTexel()
    {
        var extent = new Extent2D(512u, 256u);
        var rgba = new float[extent.Width * extent.Height * EnvironmentMap.Channels];

        var map = EnvironmentMap.FromEquirect(rgba, extent, 16);

        Assert.That(map.PanoramaExtent, Is.EqualTo(new Extent2D(128u, 64u)));
    }

    [Test]
    public void ThePanoramaKeepsItsColoursAsHalves()
    {
        var map = Build(_ => Sky);

        Assert.That(map.Panorama[..EnvironmentMap.Channels].ToArray(),
            Is.EqualTo(new[] { (Half)0.25f, (Half)0.5f, (Half)1f, (Half)1f }));
    }

    [Test]
    public void ValuesTooBrightForAHalfAreClampedInsteadOfBecomingInfinite()
    {
        var map = Build(_ => new Vector3(1e9f));

        Assert.That(Half.IsFinite(map.Panorama[0]), Is.True);
    }

    [Test]
    public void AConstantSkyGivesIrradianceOfItsColourTimesPi()
    {
        var irradiance = Build(_ => Sky).Irradiance;

        Assert.Multiple(() =>
        {
            Assert.That(Vector3.Distance(irradiance.Evaluate(Vector3.UnitY), Sky * MathF.PI), Is.LessThan(0.02f));
            for (var i = 1; i < SphericalHarmonics9.CoefficientCount; i++)
                Assert.That(irradiance[i].Length(), Is.LessThan(0.02f), $"coefficient {i}");
        });
    }

    [TestCase(1f, 0f, 0f)]
    [TestCase(-1f, 0f, 0f)]
    [TestCase(0f, 1f, 0f)]
    [TestCase(0f, -1f, 0f)]
    [TestCase(0f, 0f, 1f)]
    [TestCase(0f, 0f, -1f)]
    public void IrradianceIsStrongestFacingTheLight(float x, float y, float z)
    {
        var axis = new Vector3(x, y, z);
        var irradiance = Build(direction => Vector3.Dot(direction, axis) > 0.9f ? new Vector3(100f) : Vector3.Zero)
            .Irradiance;

        Vector3[] axes = [Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ];
        Assert.That(axes.MaxBy(candidate => irradiance.Evaluate(candidate).X), Is.EqualTo(axis));
    }

    [TestCase(8)]
    [TestCase(24)]
    public void RejectsFaceSizesThatCannotHoldARoughnessRange(int faceSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(_ => Sky, faceSize));
    }

    [Test]
    public void RejectsPixelsThatDoNotMatchTheExtent()
    {
        Assert.Throws<ArgumentException>(() => EnvironmentMap.FromEquirect(new float[7], new Extent2D(4u, 2u), 16));
    }
}
