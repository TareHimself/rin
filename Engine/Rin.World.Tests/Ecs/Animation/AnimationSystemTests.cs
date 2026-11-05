using System.Numerics;
using Rin.World.Ecs;
using Rin.World.Ecs.Animation;
using Rin.World.Ecs.Graphics;
using Rin.World.Ecs.Transforms;
using Rin.World.Mesh.Skinning;
using Rin.World.Mesh.Skinning.Animation;
using Rin.World.Tests.Skinning.Animation;
using SkinnedMesh = Rin.World.Ecs.Graphics.SkinnedMesh;

namespace Rin.World.Tests.Ecs.Animation;

public class AnimationSystemTests
{
    private EntityWorld _world = null!;
    private FakeRenderSystem _render = null!;
    private FakeMeshFactory _meshes = null!;
    private RenderFeature _feature = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new EntityWorld();
        _render = new FakeRenderSystem();
        _meshes = new FakeMeshFactory();
        _feature = new RenderFeature(_world, _render, _meshes);
    }

    [TearDown]
    public void TearDown()
    {
        _render.Dispose();
        _meshes.Dispose();
    }

    [Test]
    public void APoseSourceAdvancesByTheFrameDelta()
    {
        var source = NewPoseSource();
        Spawn(source);

        _world.Update(0.1f);

        Assert.That(source.TickCount, Is.EqualTo(1));
        Assert.That(source.TickedSeconds, Is.EqualTo(0.1f).Within(1e-6f));
    }

    [Test]
    public void TimeScaleScalesTheAnimationDelta()
    {
        var source = NewPoseSource();
        Spawn(source);

        _world.TimeScale = 2f;
        _world.Update(0.1f);
        _world.TimeScale = 0f;
        _world.Update(0.1f);

        Assert.That(source.TickedSeconds, Is.EqualTo(0.2f).Within(1e-6f));
    }

    [Test]
    public void ASharedPoseSourceAdvancesOncePerFrame()
    {
        var source = NewPoseSource();
        var handle = _feature.PoseSources.Add(source);
        Spawn(handle);
        Spawn(handle);

        _world.Update(0.1f);
        _world.Update(0.1f);

        Assert.That(source.TickCount, Is.EqualTo(2));
    }

    [Test]
    public void ADespawnedSkinnedMeshStopsAdvancing()
    {
        var source = NewPoseSource();
        var entity = Spawn(source);
        _world.Update(0.1f);

        _world.Despawn(entity);
        _world.Update(0.1f);

        Assert.That(source.TickCount, Is.EqualTo(1));
    }

    [Test]
    public void AFiredNotifyReceivesItsEntityAndIsReleased()
    {
        var (notify, handler) = TestNotify.At(0f, "Footstep");
        var node = new FakePoseNode(2);
        node.CrossedNotifies.Add(notify);
        var entity = Spawn(new AnimationGraph(TestSkeletons.WithSocket(Vector3.UnitX)) { Root = node });

        _world.Update(0.1f);

        Assert.That(handler.NotifyCount, Is.EqualTo(1));
        Assert.That(handler.LastEntity, Is.EqualTo(entity));
        Assert.That(((SingleInstanceNotifyFactory)notify.Factory).ReleaseCount, Is.EqualTo(1));
    }

    [Test]
    public void ANotifyStateBeginsAndEnds()
    {
        var (range, handler, _) = TestNotify.StateAt(0f, 1f, "Invulnerable");
        var node = new FakePoseNode(2);
        node.ActiveStates.Add(range);
        Spawn(new AnimationGraph(TestSkeletons.WithSocket(Vector3.UnitX)) { Root = node });

        _world.Update(0.1f);
        node.ActiveStates.Clear();
        _world.Update(0.1f);

        Assert.That(handler.BeginCount, Is.EqualTo(1));
        Assert.That(handler.EndCount, Is.EqualTo(1));
    }

    [Test]
    public void ANotifyMayDespawnItsEntityDirectly()
    {
        var notify = new AnimationNotify(0f, new DespawnNotifyFactory());
        var node = new FakePoseNode(2);
        node.CrossedNotifies.Add(notify);
        var entity = Spawn(new AnimationGraph(TestSkeletons.WithSocket(Vector3.UnitX)) { Root = node });

        _world.Update(0.1f);

        Assert.That(_world.IsAlive(entity), Is.False);
        Assert.That(_render.LiveCount, Is.Zero);
    }

    [Test]
    public void AnimationSocketsAndExtractDoNotAllocatePerFrame()
    {
        for (var i = 0; i < 100; i++)
        {
            var parent = Spawn(NewPoseSource());
            _world.SetPosition(parent, new Vector3(i, 0, 0));
            var child = _world.Spawn();
            _world.SetPosition(child, Vector3.UnitY);
            _feature.AttachToBone(child, parent, TestSkeletons.SocketBone);
        }

        _world.Update(0.1f);
        _world.Update(0.1f);
        _render.PushedPoses.Capacity = _render.PushedPoses.Count + 100 * 100;

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 100; frame++) _world.Update(0.1f);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    private static FakePoseSource NewPoseSource() =>
        new() { Skeleton = TestSkeletons.WithSocket(Vector3.UnitX), Pose = new SkeletalPose(2) };

    private Entity Spawn(IPoseSource source) => Spawn(_feature.PoseSources.Add(source));

    private Entity Spawn(PoseSourceHandle handle)
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, Vector3.Zero);
        _world.Add(entity, new SkinnedMesh { MeshId = _meshes.Register(new FakeMesh()), PoseSource = handle });
        return entity;
    }

    private sealed class DespawnNotifyFactory : IAnimationNotifyFactory
    {
        public IAnimationNotify Create() => new DespawnNotify();

        public void Release(IAnimationNotify instance)
        {
        }
    }

    private sealed class DespawnNotify : IAnimationNotify
    {
        public void Reset()
        {
        }

        public void Notify(EntityWorld world, Entity entity, AnimationGraph graph) => world.Despawn(entity);
    }
}
