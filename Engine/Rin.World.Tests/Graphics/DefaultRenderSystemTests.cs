using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;

namespace Rin.World.Tests.Graphics;

/// <summary>Exercises the real <see cref="DefaultRenderSystem" />'s fixed-timestep render interpolation.</summary>
public class DefaultRenderSystemTests
{
    private static RenderProxyHandle CreateProxy(DefaultRenderSystem render, in Matrix4x4 transform)
    {
        var handle = render.CreateStaticMeshProxy(new StaticMeshProxyDesc
        {
            Mesh = new FakeMesh(),
            Transform = transform,
            SurfaceIndices = [],
            Materials = []
        });
        render.Snapshot(TestCameras.Identity, new Extent2D(1, 1)); // drains the creation command
        return handle;
    }

    [Test]
    public void SnapshotReturnsCurrentTransformWhenAlphaIsOne()
    {
        var render = new DefaultRenderSystem();
        var start = Matrix4x4.CreateTranslation(1, 2, 3);
        var handle = CreateProxy(render, start);
        var moved = Matrix4x4.CreateTranslation(4, 5, 6);

        render.UpdateProxyTransform(handle, moved);
        render.SetInterpolationAlpha(1f);
        var context = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        Assert.That(context.Snapshot.StaticGeometry[0].Transform, Is.EqualTo(moved));
    }

    [Test]
    public void SnapshotInterpolatesBetweenLastTwoDistinctTransforms()
    {
        var render = new DefaultRenderSystem();
        var start = Matrix4x4.CreateTranslation(0, 0, 0);
        var handle = CreateProxy(render, start);
        var target = Matrix4x4.CreateTranslation(10, 0, 0);

        render.UpdateProxyTransform(handle, target);
        render.SetInterpolationAlpha(0.5f);
        var context = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        Assert.That(context.Snapshot.StaticGeometry[0].Transform.Translation.X, Is.EqualTo(5f).Within(1e-4f),
            "at alpha 0.5 the rendered position should sit halfway between the last two committed positions");
    }

    [Test]
    public void RepeatedIdenticalPushesDoNotResetTheInterpolationWindow()
    {
        // Mirrors SingleBodyPhysicsComponent.Update re-pushing the same pose every frame regardless of stepping.
        var render = new DefaultRenderSystem();
        var start = Matrix4x4.CreateTranslation(0, 0, 0);
        var handle = CreateProxy(render, start);

        for (var i = 0; i < 5; i++) render.UpdateProxyTransform(handle, start);

        var target = Matrix4x4.CreateTranslation(10, 0, 0);
        render.UpdateProxyTransform(handle, target);
        render.SetInterpolationAlpha(0.5f);
        var context = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        Assert.That(context.Snapshot.StaticGeometry[0].Transform.Translation.X, Is.EqualTo(5f).Within(1e-4f),
            "re-pushing the same pose several times should not have shifted Previous forward each time");
    }

    [Test]
    public void ATransformPushedInAFrameWithoutATickIsDrawnAtOnce()
    {
        var render = new DefaultRenderSystem();
        var handle = CreateProxy(render, Matrix4x4.Identity);
        var target = Matrix4x4.CreateTranslation(10, 0, 0);

        render.BeginFrame(ticked: false);
        render.UpdateProxyTransform(handle, target);
        render.SetInterpolationAlpha(0f);
        var context = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        Assert.That(context.Snapshot.StaticGeometry[0].Transform, Is.EqualTo(target));
    }

    [Test]
    public void ATickStartsANewBlendSoARowThatStoppedMovingIsDrawnWhereItStopped()
    {
        var render = new DefaultRenderSystem();
        var handle = CreateProxy(render, Matrix4x4.Identity);
        render.BeginFrame(ticked: true);
        render.UpdateProxyTransform(handle, Matrix4x4.CreateTranslation(10, 0, 0));
        render.SetInterpolationAlpha(0.5f);
        var moving = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));
        var movingX = moving.Snapshot.StaticGeometry[0].Transform.Translation.X;

        render.BeginFrame(ticked: true);
        var stopped = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        Assert.That(movingX, Is.EqualTo(5f).Within(1e-4f));
        Assert.That(stopped.Snapshot.StaticGeometry[0].Transform.Translation.X, Is.EqualTo(10f).Within(1e-4f));
    }

    [Test]
    public void AFrameWithNothingToSettleKeepsTheSameScene()
    {
        var render = new DefaultRenderSystem();
        CreateProxy(render, Matrix4x4.Identity);
        var first = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        render.BeginFrame(ticked: false);
        render.BeginFrame(ticked: true);
        var second = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        Assert.That(second.Snapshot, Is.SameAs(first.Snapshot));
    }

    [Test]
    public void SnapshotSlerpsRotationInsteadOfLinearlyBlendingTheMatrix()
    {
        var render = new DefaultRenderSystem();
        var start = Matrix4x4.CreateFromQuaternion(Quaternion.Identity);
        var handle = CreateProxy(render, start);
        var target = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f));

        render.UpdateProxyTransform(handle, target);
        render.SetInterpolationAlpha(0.5f);
        var context = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        var ok = Matrix4x4.Decompose(context.Snapshot.StaticGeometry[0].Transform, out var scale, out var rotation, out _);
        Assert.That(ok, Is.True, "a naive per-element matrix lerp would decompose into a skewed, non-rotation matrix here");
        Assert.That(Vector3.Distance(scale, Vector3.One), Is.LessThan(1e-4f),
            "interpolated scale should stay exactly 1 - a linearly-blended rotation submatrix would shrink it partway through the blend");

        var expected = Quaternion.Slerp(Quaternion.Identity, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f), 0.5f);
        Assert.That(Quaternion.Dot(rotation, expected), Is.EqualTo(1f).Within(1e-4f),
            "rotation should match a quaternion Slerp at the same alpha");
    }

    [Test]
    public void SnapshotsWithoutChangesShareOneScene()
    {
        var render = new DefaultRenderSystem();
        CreateProxy(render, Matrix4x4.Identity);

        var first = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));
        var second = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(2, 2));

        Assert.That(second.Snapshot, Is.SameAs(first.Snapshot));
    }

    [Test]
    public void ProxyChangesProduceANewScene()
    {
        var render = new DefaultRenderSystem();
        var handle = CreateProxy(render, Matrix4x4.Identity);
        var before = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        render.UpdateProxyTransform(handle, Matrix4x4.CreateTranslation(1, 0, 0));
        var after = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        Assert.That(after.Snapshot, Is.Not.SameAs(before.Snapshot));
    }

    private static StaticMeshInfo[] StaticGeometry(DefaultRenderSystem render)
    {
        return ((DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1))).Snapshot
            .StaticGeometry.ToArray();
    }

    [Test]
    public void DestroyedProxyLeavesTheScene()
    {
        var render = new DefaultRenderSystem();
        var handle = CreateProxy(render, Matrix4x4.Identity);

        render.DestroyProxy(handle);

        Assert.That(StaticGeometry(render), Is.Empty);
    }

    [Test]
    public void CommandsApplyInTheOrderTheyWereQueued()
    {
        var render = new DefaultRenderSystem();
        var handle = CreateProxy(render, Matrix4x4.Identity);
        var last = Matrix4x4.CreateTranslation(2, 0, 0);

        render.UpdateProxyTransform(handle, Matrix4x4.CreateTranslation(1, 0, 0));
        render.UpdateProxyTransform(handle, last);

        Assert.That(StaticGeometry(render)[0].Transform, Is.EqualTo(last));
    }

    [Test]
    public void AStaleHandleDoesNotTouchTheProxyThatReusedItsSlot()
    {
        var render = new DefaultRenderSystem();
        var stale = CreateProxy(render, Matrix4x4.Identity);
        render.DestroyProxy(stale);
        StaticGeometry(render);
        var start = Matrix4x4.CreateTranslation(5, 0, 0);
        CreateProxy(render, start);

        render.UpdateProxyTransform(stale, Matrix4x4.CreateTranslation(9, 0, 0));
        render.DestroyProxy(stale);

        var geometry = StaticGeometry(render);
        Assert.That(geometry, Has.Length.EqualTo(1));
        Assert.That(geometry[0].Transform, Is.EqualTo(start));
    }

    [Test]
    public void InterpolationAlphaChangesProduceANewScene()
    {
        var render = new DefaultRenderSystem();
        CreateProxy(render, Matrix4x4.Identity);
        var before = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        render.SetInterpolationAlpha(0.5f);
        var after = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        Assert.That(after.Snapshot, Is.Not.SameAs(before.Snapshot));
    }
}
