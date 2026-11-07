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

    private static float DepthAt(Matrix4x4 projection, float viewZ)
    {
        var clip = Vector4.Transform(new Vector4(0f, 0f, viewZ, 1f), projection);
        return clip.Z / clip.W;
    }

    [Test]
    public void PerspectiveProjection_maps_near_to_one_and_falls_toward_zero()
    {
        const float near = 2f;
        var projection = MathR.PerspectiveProjection(90f, 1920f, 1080f, near);

        Assert.Multiple(() =>
        {
            Assert.That(DepthAt(projection, near), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(DepthAt(projection, near * 2f), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(DepthAt(projection, 1e6f), Is.GreaterThan(0f).And.LessThan(1e-5f));
            Assert.That(DepthAt(projection, near * 0.5f), Is.GreaterThan(1f), "in front of near is clipped");
        });
    }

    [Test]
    public void PerspectiveProjection_maps_the_vertical_field_of_view_edge_to_the_clip_edge()
    {
        var projection = MathR.PerspectiveProjection(90f, 200f, 100f, 0.1f);

        var top = Vector4.Transform(new Vector4(0f, 10f, 10f, 1f), projection);
        var right = Vector4.Transform(new Vector4(20f, 0f, 10f, 1f), projection);

        Assert.Multiple(() =>
        {
            Assert.That(top.Y / top.W, Is.EqualTo(-1f).Within(1e-5f), "up is -Y in clip space");
            Assert.That(right.X / right.W, Is.EqualTo(1f).Within(1e-5f));
        });
    }

    private static bool Inside(Vector4 plane, Vector3 point) => Vector4.Dot(plane, new Vector4(point, 1f)) >= 0f;

    // A camera at (5, 1, -3) turned to look along +X, so its right is -Z and its up is +Y.
    private static (Frustum Frustum, Vector3 Eye) TurnedCamera(float near, float far)
    {
        var eye = new Vector3(5f, 1f, -3f);
        var cameraToWorld = Matrix4x4.CreateRotationY(float.Pi / 2f) * Matrix4x4.CreateTranslation(eye);
        Matrix4x4.Invert(cameraToWorld, out var view);
        var viewProjection = view * MathR.PerspectiveProjection(90f, 100f, 100f, near);
        return (MathR.ExtractWorldSpaceFrustum(view, viewProjection, far), eye);
    }

    [Test]
    public void ExtractWorldSpaceFrustum_contains_points_in_view()
    {
        var (frustum, eye) = TurnedCamera(0.1f, 50f);
        Vector3[] inView = [eye + new Vector3(10f, 0f, 0f), eye + new Vector3(10f, 9f, -9f), eye + new Vector3(49f, 0f, 0f)];

        foreach (var point in inView)
            for (var i = 0; i < 6; i++)
                Assert.That(Inside(frustum[i], point), $"{point} should be inside plane {i}");
    }

    [Test]
    public void ExtractWorldSpaceFrustum_planes_exclude_the_matching_side()
    {
        var (frustum, eye) = TurnedCamera(0.1f, 50f);

        Assert.Multiple(() =>
        {
            Assert.That(Inside(frustum.Near, eye + new Vector3(-1f, 0f, 0f)), Is.False, "behind the camera");
            Assert.That(Inside(frustum.Near, eye + new Vector3(0.05f, 0f, 0f)), Is.False, "closer than near");
            Assert.That(Inside(frustum.Far, eye + new Vector3(51f, 0f, 0f)), Is.False, "beyond far");
            Assert.That(Inside(frustum.Right, eye + new Vector3(10f, 0f, -11f)), Is.False, "right of view");
            Assert.That(Inside(frustum.Left, eye + new Vector3(10f, 0f, 11f)), Is.False, "left of view");
            Assert.That(Inside(frustum.Top, eye + new Vector3(10f, 11f, 0f)), Is.False, "above view");
            Assert.That(Inside(frustum.Bottom, eye + new Vector3(10f, -11f, 0f)), Is.False, "below view");
        });
    }
}
