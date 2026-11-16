using System.Numerics;
using Rin.World.Graphics;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics;

public class EnvironmentLightingTests
{
    private static readonly Vector3 UnitBox = Vector3.One;

    private static EnvironmentVolumeInfo Box(Matrix4x4 localToWorld)
    {
        Matrix4x4.Invert(localToWorld, out var worldToLocal);
        return new EnvironmentVolumeInfo { LocalToWorld = localToWorld, WorldToLocal = worldToLocal, Extent = UnitBox };
    }

    private static void AssertClose(Vector3 actual, Vector3 expected, float tolerance = 1e-5f)
    {
        Assert.That(Vector3.Distance(actual, expected), Is.LessThan(tolerance), $"{actual} vs {expected}");
    }

    [Test]
    public void ABoxAppliesFullyInsideAndNotAtAllOutside()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EnvironmentLighting.BoxWeight(Vector3.Zero, UnitBox, 0f), Is.EqualTo(1f));
            Assert.That(EnvironmentLighting.BoxWeight(new Vector3(0.99f, 0f, 0f), UnitBox, 0f), Is.EqualTo(1f));
            Assert.That(EnvironmentLighting.BoxWeight(new Vector3(1.01f, 0f, 0f), UnitBox, 0f), Is.EqualTo(0f));
            Assert.That(EnvironmentLighting.BoxWeight(new Vector3(0f, -1.01f, 0f), UnitBox, 0f), Is.EqualTo(0f));
        });
    }

    [Test]
    public void TheWeightRampsUpOverTheBlendDistanceFromTheNearestFace()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EnvironmentLighting.BoxWeight(new Vector3(0.9f, 0f, 0f), UnitBox, 0.4f),
                Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(EnvironmentLighting.BoxWeight(new Vector3(0.9f, -0.8f, 0f), UnitBox, 0.4f),
                Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(EnvironmentLighting.BoxWeight(new Vector3(0.2f, 0f, 0f), UnitBox, 0.4f), Is.EqualTo(1f));
        });
    }

    [Test]
    public void FromTheCentreOfABoxALookupDirectionIsUnchanged()
    {
        var direction = Vector3.Normalize(new Vector3(0.3f, -0.5f, 0.8f));

        AssertClose(EnvironmentLighting.ProjectOntoBox(Box(Matrix4x4.Identity), Vector3.Zero, direction), direction);
    }

    [Test]
    public void AwayFromTheCentreTheDirectionPointsAtTheWallTheRayHits()
    {
        var box = Box(Matrix4x4.Identity);
        var offCentre = new Vector3(0.5f, 0f, 0f);

        Assert.Multiple(() =>
        {
            AssertClose(EnvironmentLighting.ProjectOntoBox(box, offCentre, Vector3.UnitX), Vector3.UnitX);
            AssertClose(EnvironmentLighting.ProjectOntoBox(box, offCentre, Vector3.UnitY),
                Vector3.Normalize(new Vector3(0.5f, 1f, 0f)));
            AssertClose(EnvironmentLighting.ProjectOntoBox(box, offCentre, -Vector3.UnitZ),
                Vector3.Normalize(new Vector3(0.5f, 0f, -1f)));
        });
    }

    [Test]
    public void ARotatedBoxProjectsInItsOwnSpaceAndAnswersInWorldSpace()
    {
        var quarterTurn = Matrix4x4.CreateRotationY(MathF.PI / 2f);
        var box = Box(quarterTurn);
        var local = new Vector3(0.5f, 0f, 0f);
        var worldUp = Vector3.UnitY;

        var projected = EnvironmentLighting.ProjectOntoBox(box, local, worldUp);

        AssertClose(projected, Vector3.Normalize(Vector3.TransformNormal(new Vector3(0.5f, 1f, 0f), quarterTurn)));
    }

    [Test]
    public void AScaledBoxHitsItsWallsWhereTheyAreInTheWorld()
    {
        var box = Box(Matrix4x4.CreateScale(4f));

        var projected = EnvironmentLighting.ProjectOntoBox(box, new Vector3(0.5f, 0f, 0f), Vector3.UnitY);

        AssertClose(projected, Vector3.Normalize(new Vector3(0.5f, 1f, 0f)));
    }

    [Test]
    public void IrradianceInAShaderMatchesTheCoefficientsEvaluatedOnTheCpu()
    {
        var texels = new Vector4[64 * 32];
        texels.AsSpan(0, 64 * 16).Fill(new Vector4(1f, 2f, 3f, 1f));
        var irradiance = Panorama.ProjectIrradiance(texels, 64, 32);
        var coefficients = irradiance.ToCoefficients();

        Assert.Multiple(() =>
        {
            foreach (var normal in new[] { Vector3.UnitY, Vector3.UnitX, Vector3.Normalize(new Vector3(1f, 2f, -3f)) })
                AssertClose(EnvironmentLighting.Irradiance(coefficients, normal), irradiance.Evaluate(normal), 1e-4f);
        });
    }

    [Test]
    public void IrradianceNeverGoesNegative()
    {
        var texels = new Vector4[64 * 32];
        texels.AsSpan(0, 64 * 4).Fill(new Vector4(50f));
        var coefficients = Panorama.ProjectIrradiance(texels, 64, 32).ToCoefficients();

        var facingAway = EnvironmentLighting.Irradiance(coefficients, -Vector3.UnitY);

        Assert.That(float.Min(facingAway.X, float.Min(facingAway.Y, facingAway.Z)), Is.GreaterThanOrEqualTo(0f));
    }
}
