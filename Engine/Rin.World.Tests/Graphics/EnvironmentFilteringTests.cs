using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics;

public class EnvironmentFilteringTests
{
    [TestCase(CubeFace.PositiveX, 1f, 0f, 0f)]
    [TestCase(CubeFace.NegativeX, -1f, 0f, 0f)]
    [TestCase(CubeFace.PositiveY, 0f, 1f, 0f)]
    [TestCase(CubeFace.NegativeY, 0f, -1f, 0f)]
    [TestCase(CubeFace.PositiveZ, 0f, 0f, 1f)]
    [TestCase(CubeFace.NegativeZ, 0f, 0f, -1f)]
    public void AFaceCentreLooksAlongItsAxis(CubeFace face, float x, float y, float z)
    {
        Assert.That(EnvironmentFiltering.FaceDirection((int)face, 0f, 0f), Is.EqualTo(new Vector3(x, y, z)));
    }

    [TestCase(CubeFace.PositiveX)]
    [TestCase(CubeFace.NegativeX)]
    [TestCase(CubeFace.PositiveZ)]
    [TestCase(CubeFace.NegativeZ)]
    public void SideFacesHaveUpAtTheTopOfTheImage(CubeFace face)
    {
        Assert.That(EnvironmentFiltering.FaceDirection((int)face, 0f, -0.5f).Y, Is.GreaterThan(0f));
    }

    [Test]
    public void TheForwardFaceHasRightOnTheRightOfTheImage()
    {
        Assert.That(EnvironmentFiltering.FaceDirection((int)CubeFace.PositiveZ, 0.5f, 0f).X, Is.GreaterThan(0f));
    }

    [Test]
    public void EveryFaceDirectionIsUnitLength()
    {
        Assert.Multiple(() =>
        {
            foreach (var face in Enum.GetValues<CubeFace>())
                Assert.That(EnvironmentFiltering.FaceDirection((int)face, 0.7f, -0.9f).Length(),
                    Is.EqualTo(1f).Within(1e-5f), face.ToString());
        });
    }

    [TestCase(0.5f, 0.5f)]
    [TestCase(0.75f, 0.5f)]
    [TestCase(0.1f, 0.3f)]
    [TestCase(0.9f, 0.8f)]
    public void ADirectionLandsBackWhereThePanoramaShowsIt(float u, float v)
    {
        var coordinate = EnvironmentFiltering.PanoramaCoordinate(Panorama.Direction(u, v));

        Assert.That(Vector2.Distance(coordinate, new Vector2(u, v)), Is.LessThan(1e-4f));
    }
}
