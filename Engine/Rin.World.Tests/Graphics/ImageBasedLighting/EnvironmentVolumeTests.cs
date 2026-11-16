using System.Numerics;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

public class EnvironmentVolumeTests
{
    private static readonly Vector3 Centre = new(10f, 0f, 0f);

    private static EnvironmentVolume Box(float blendDistance = 0f)
    {
        return TestEnvironments.Box(1, Vector3.One, Centre, 4f, blendDistance);
    }

    [Test]
    public void AnUnboundedVolumeAppliesEverywhere()
    {
        var volume = TestEnvironments.Unbounded(1, Vector3.One);

        Assert.Multiple(() =>
        {
            Assert.That(volume.IsUnbounded, Is.True);
            Assert.That(volume.WeightAt(new Vector3(1e6f)), Is.EqualTo(1f));
        });
    }

    [Test]
    public void ABoxAppliesInsideAndNotOutside()
    {
        var box = Box();

        Assert.Multiple(() =>
        {
            Assert.That(box.WeightAt(Centre), Is.EqualTo(1f));
            Assert.That(box.WeightAt(Centre + new Vector3(3.9f, 0f, 0f)), Is.EqualTo(1f));
            Assert.That(box.WeightAt(Centre + new Vector3(4.1f, 0f, 0f)), Is.EqualTo(0f));
            Assert.That(box.WeightAt(Vector3.Zero), Is.EqualTo(0f));
        });
    }

    [Test]
    public void TheWeightRampsUpOverTheBlendDistance()
    {
        var box = Box(2f);

        Assert.Multiple(() =>
        {
            Assert.That(box.WeightAt(Centre + new Vector3(4f, 0f, 0f)), Is.EqualTo(0f));
            Assert.That(box.WeightAt(Centre + new Vector3(3f, 0f, 0f)), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(box.WeightAt(Centre + new Vector3(2f, 0f, 0f)), Is.EqualTo(1f));
            Assert.That(box.WeightAt(Centre), Is.EqualTo(1f));
        });
    }

    [Test]
    public void TheNearestFaceDecidesTheWeight()
    {
        var box = Box(2f);

        Assert.That(box.WeightAt(Centre + new Vector3(0f, 3.5f, 1f)), Is.EqualTo(0.25f).Within(1e-5f));
    }

    [Test]
    public void TheBoundingSphereReachesTheBoxCorners()
    {
        var box = Box();
        box.Extent = new Vector3(1f, 2f, 2f);

        Assert.Multiple(() =>
        {
            Assert.That(box.Center, Is.EqualTo(Centre));
            Assert.That(box.BoundingRadius, Is.EqualTo(3f).Within(1e-5f));
        });
    }

    [Test]
    public void TheBoundingSphereGrowsWithTheEntityScaleAndIgnoresRotation()
    {
        var box = Box();
        box.Extent = new Vector3(1f, 2f, 2f);
        box.Transform = Matrix4x4.CreateScale(2f) * Matrix4x4.CreateRotationY(0.7f) * Matrix4x4.CreateTranslation(Centre);

        Assert.That(box.BoundingRadius, Is.GreaterThanOrEqualTo(6f - 1e-4f));
    }

    [Test]
    public void TheShaderInfoCarriesTheBoxAndItsMapScaledByIntensity()
    {
        var box = TestEnvironments.Box(7, new Vector3(1f, 2f, 3f), Centre, 4f, 2f, 5);
        box.Intensity = 2f;

        var info = box.ToInfo();

        Assert.Multiple(() =>
        {
            Assert.That(info.LocalToWorld, Is.EqualTo(box.Transform));
            Assert.That(Vector4.Transform(new Vector4(Centre, 1f), info.WorldToLocal),
                Is.EqualTo(new Vector4(0f, 0f, 0f, 1f)));
            Assert.That(info.Extent, Is.EqualTo(new Vector3(4f)));
            Assert.That(info.BlendDistance, Is.EqualTo(2f));
            Assert.That(info.Priority, Is.EqualTo(5));
            Assert.That(info.Irradiance.L00, Is.EqualTo(new Vector3(2f, 4f, 6f)));
            Assert.That(info.Reflection.Cubemap.Id, Is.EqualTo(7u));
            Assert.That(info.Reflection.MaxMip, Is.EqualTo(1f));
            Assert.That(info.Reflection.Intensity, Is.EqualTo(2f));
        });
    }

    [Test]
    public void TheBoxFollowsTheEntityRotation()
    {
        var box = Box();
        box.Extent = new Vector3(8f, 1f, 1f);
        box.Transform = Matrix4x4.CreateRotationY(MathF.PI / 2f) * Matrix4x4.CreateTranslation(Centre);

        Assert.Multiple(() =>
        {
            Assert.That(box.WeightAt(Centre + new Vector3(0f, 0f, 6f)), Is.EqualTo(1f));
            Assert.That(box.WeightAt(Centre + new Vector3(6f, 0f, 0f)), Is.EqualTo(0f));
        });
    }
}
