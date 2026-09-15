using System.Numerics;
using Rin.Core.Shared.Math;

namespace Rin.Core.Tests.Shared;

public class MathRTests
{
    private static void AssertVectorsClose(Vector3 actual, Vector3 expected, float tolerance = 1e-4f)
    {
        Assert.That((actual - expected).Length(), Is.LessThan(tolerance),
            $"expected {expected} but got {actual}");
    }

    [Test]
    public void LookTowards_orients_forward_along_the_direction()
    {
        Vector3[] dirs =
        [
            MathR.Forward,
            new Vector3(0f, -1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(0f, 0f, -1f),
            Vector3.Normalize(new Vector3(1f, 2f, 3f)),
            Vector3.Normalize(new Vector3(-4f, 0.5f, 1f))
        ];

        foreach (var dir in dirs)
        {
            var q = MathR.LookTowards(dir);
            AssertVectorsClose(Vector3.Transform(MathR.Forward, q), Vector3.Normalize(dir));
        }
    }

    [Test]
    public void LookTowards_is_roll_free()
    {
        // The right vector must stay horizontal (no roll) for any non-vertical direction.
        foreach (var dir in new[]
                 {
                     new Vector3(1f, 0f, 0f),
                     Vector3.Normalize(new Vector3(1f, 0.3f, -2f)),
                     Vector3.Normalize(new Vector3(-3f, -0.8f, 1f))
                 })
        {
            var right = Vector3.Transform(MathR.Right, MathR.LookTowards(dir));
            Assert.That(MathF.Abs(right.Y), Is.LessThan(1e-4f), $"roll introduced for {dir}");
        }
    }

    [Test]
    public void LookTowards_matches_the_camera_controller_composition()
    {
        // Forward.ToQuaternion() is identity, so the controller chain reduces to yaw then local pitch.
        const float yawDeg = 40f;
        const float pitchDeg = -25f;

        var chained = MathR.Forward.ToQuaternion().AddYaw(yawDeg).AddLocalPitch(pitchDeg);
        var viaLookTowards = MathR.LookTowards(Vector3.Transform(MathR.Forward, chained));

        AssertVectorsClose(Vector3.Transform(MathR.Forward, viaLookTowards),
            Vector3.Transform(MathR.Forward, chained));
    }

    [Test]
    public void LookTowards_zero_returns_identity()
    {
        Assert.That(MathR.LookTowards(Vector3.Zero), Is.EqualTo(Quaternion.Identity));
    }
}
