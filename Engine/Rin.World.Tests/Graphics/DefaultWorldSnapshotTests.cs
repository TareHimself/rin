using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Tests.Graphics;

public class DefaultWorldSnapshotTests
{
    private static readonly Extent2D Extent = new(1, 1);
    private DefaultRenderSystem _render = null!;

    [SetUp]
    public void CreateRenderSystem()
    {
        _render = new DefaultRenderSystem();
    }

    [TearDown]
    public void DisposeRenderSystem()
    {
        _render.Dispose();
    }

    private DefaultWorldViewData View()
    {
        return (DefaultWorldViewData)_render.Snapshot(TestCameras.Identity, Extent);
    }

    private RenderProxyHandle AddStaticMesh()
    {
        return _render.CreateStaticMeshProxy(new StaticMeshProxyDesc
        {
            Mesh = new FakeMesh(),
            Transform = Matrix4x4.Identity,
            SurfaceIndices = [],
            Materials = []
        });
    }

    private void AddSkinnedMesh(Skeleton skeleton, in SkeletalPose pose)
    {
        _render.CreateSkinnedMeshProxy(new SkinnedMeshProxyDesc
        {
            Skeleton = skeleton,
            Pose = pose,
            Mesh = new FakeMesh(),
            Transform = Matrix4x4.Identity,
            SurfaceIndices = [],
            Materials = []
        });
    }

    /// <summary>
    ///     Makes the next snapshot rebuild the scene.
    /// </summary>
    private void ChangeTheScene()
    {
        AddStaticMesh();
    }

    private static Vector3 FirstBonePosition(DefaultWorldSnapshot scene)
    {
        return scene.BonesOf(scene.SkinnedGeometry[0])[0].Position;
    }

    [Test]
    public void APoseChangedAfterTheSnapshotDoesNotReachIt()
    {
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var pose = new SkeletalPose(1);
        pose.Set(0, new Transform { Position = new Vector3(1, 0, 0) });
        AddSkinnedMesh(skeleton, pose);
        var view = View();

        pose.Set(0, new Transform { Position = new Vector3(9, 0, 0) });

        Assert.That(FirstBonePosition(view.Snapshot), Is.EqualTo(new Vector3(1, 0, 0)));
    }

    [Test]
    public void ABoneThePoseDoesNotSetReadsAsTheSkeletonsLocalTransform()
    {
        var local = new Transform { Position = new Vector3(0, 4, 0) };
        var skeleton = new Skeleton([new Bone { Name = "root", LocalTransform = local }]);
        AddSkinnedMesh(skeleton, new SkeletalPose(1));

        Assert.That(FirstBonePosition(View().Snapshot), Is.EqualTo(local.Position));
    }

    [Test]
    public void EachSkinnedMeshReadsItsOwnBones()
    {
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var first = new SkeletalPose(1);
        first.Set(0, new Transform { Position = new Vector3(1, 0, 0) });
        var second = new SkeletalPose(1);
        second.Set(0, new Transform { Position = new Vector3(2, 0, 0) });
        AddSkinnedMesh(skeleton, first);
        AddSkinnedMesh(skeleton, second);

        var scene = View().Snapshot;

        Assert.That(scene.BonesOf(scene.SkinnedGeometry[1])[0].Position, Is.EqualTo(new Vector3(2, 0, 0)));
    }

    [Test]
    public void ASceneIsReusedOnceEveryHolderHasLetGo()
    {
        AddStaticMesh();
        var view = View();
        var scene = view.Snapshot;
        ChangeTheScene();
        View().Dispose();

        view.Dispose();
        ChangeTheScene();

        Assert.That(View().Snapshot, Is.SameAs(scene));
    }

    [Test]
    public void ASceneAViewStillHoldsIsNotReused()
    {
        AddStaticMesh();
        var held = View();
        ChangeTheScene();
        View().Dispose();
        ChangeTheScene();

        var next = View();

        Assert.Multiple(() =>
        {
            Assert.That(next.Snapshot, Is.Not.SameAs(held.Snapshot));
            Assert.That(held.Snapshot.StaticGeometry.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void AReusedSceneHoldsNothingFromItsPreviousUse()
    {
        var handle = AddStaticMesh();
        AddSkinnedMesh(new Skeleton([new Bone { Name = "root" }]), new SkeletalPose(1));
        var view = View();
        var scene = view.Snapshot;
        _render.DestroyProxy(handle);
        View().Dispose();
        view.Dispose();
        ChangeTheScene();

        var reused = View().Snapshot;

        Assert.Multiple(() =>
        {
            Assert.That(reused, Is.SameAs(scene));
            Assert.That(reused.StaticGeometry.Length, Is.EqualTo(1));
            Assert.That(reused.SkinnedGeometry.Length, Is.EqualTo(1));
            Assert.That(reused.BonesOf(reused.SkinnedGeometry[0]).Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void DisposingAViewTwiceLetsGoOnce()
    {
        AddStaticMesh();
        var first = View();
        var second = View();

        first.Dispose();
        first.Dispose();
        ChangeTheScene();
        View().Dispose();

        Assert.DoesNotThrow(() => _ = second.Snapshot.StaticGeometry.Length);
    }

    [Test]
    public void ReadingASceneNothingHoldsThrows()
    {
        AddStaticMesh();
        var view = View();
        var scene = view.Snapshot;
        ChangeTheScene();
        View().Dispose();

        view.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = scene.StaticGeometry.Length);
    }

    [Test]
    public void TheRenderSystemLetsGoOfItsSceneWhenDisposed()
    {
        AddStaticMesh();
        var view = View();
        var scene = view.Snapshot;
        view.Dispose();

        _render.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = scene.StaticGeometry.Length);
    }
}
