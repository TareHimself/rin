using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Graphics;
using Rin.World.Transforms;
using Rin.World.Mesh.Skinning;
using SkinnedMesh = Rin.World.Graphics.SkinnedMesh;

namespace Rin.World.Tests.Animation;

public class BoneSocketSyncTests
{
    private World _world = null!;
    private FakeRenderSystem _render = null!;
    private FakeMeshFactory _meshes = null!;
    private RenderFeature _feature = null!;
    private FakePoseSource _source = null!;
    private Entity _parent;

    [SetUp]
    public void SetUp()
    {
        _world = new World();
        _render = new FakeRenderSystem();
        _meshes = new FakeMeshFactory();
        _feature = new RenderFeature(_world, _render, _meshes);

        _source = new FakePoseSource { Skeleton = TestSkeletons.WithSocket(new Vector3(2, 0, 0)), Pose = new SkeletalPose(2) };
        _parent = _world.Spawn();
        _world.SetPosition(_parent, new Vector3(10, 0, 0));
        _world.Add(_parent, new SkinnedMesh { PoseSource = _feature.PoseSources.Add(_source) });
    }

    [TearDown]
    public void TearDown()
    {
        _render.Dispose();
        _meshes.Dispose();
    }

    [Test]
    public void ASocketedChildIsPlacedAtTheBoneOfItsParent()
    {
        var child = SpawnSocketedChild(new Vector3(0, 1, 0));

        _world.Update(0f);

        Assert.That(WorldPosition(child), Is.EqualTo(new Vector3(12, 1, 0)));
    }

    [Test]
    public void TheSocketAppliesBeforeTheFirstPropagation()
    {
        var child = SpawnSocketedChild(new Vector3(0, 1, 0));

        Assert.That(_world.ComputeWorldMatrix(child).Translation, Is.EqualTo(new Vector3(12, 1, 0)));
    }

    [Test]
    public void ASocketedChildFollowsTheBoneWhenThePoseChanges()
    {
        var child = SpawnSocketedChild(Vector3.Zero);
        _world.Update(0f);

        _source.Pose.Set(1, new Transform { Position = new Vector3(5, 0, 0) });
        _world.Update(0f);

        Assert.That(WorldPosition(child), Is.EqualTo(new Vector3(15, 0, 0)));
    }

    [Test]
    public void ASocketedChildFollowsItsParentMoving()
    {
        var child = SpawnSocketedChild(Vector3.Zero);
        _world.Update(0f);

        _world.SetPosition(_parent, new Vector3(0, 0, 3));
        _world.Update(0f);

        Assert.That(WorldPosition(child), Is.EqualTo(new Vector3(2, 0, 3)));
    }

    [Test]
    public void AGrandchildFollowsTheSocketedChild()
    {
        var child = SpawnSocketedChild(Vector3.Zero);
        var grandchild = _world.Spawn();
        _world.SetPosition(grandchild, new Vector3(0, 0, 1));
        _world.SetParent(grandchild, child);
        _world.Update(0f);

        _source.Pose.Set(1, new Transform { Position = new Vector3(4, 0, 0) });
        _world.Update(0f);

        Assert.That(WorldPosition(grandchild), Is.EqualTo(new Vector3(14, 0, 1)));
    }

    [Test]
    public void RemovingTheSocketPlacesTheChildAtItsParentAgain()
    {
        var child = SpawnSocketedChild(Vector3.Zero);
        _world.Update(0f);

        _world.Remove<AttachSocket>(child);
        _world.Update(0f);

        Assert.That(WorldPosition(child), Is.EqualTo(new Vector3(10, 0, 0)));
    }

    [Test]
    public void ReattachingToAnotherBoneOfTheSameParentMovesTheChildWithAStaticPose()
    {
        var child = SpawnSocketedChild(Vector3.Zero);
        _world.Update(0f);

        _feature.AttachToBone(child, _parent, "root");
        _world.Update(0f);

        Assert.That(WorldPosition(child), Is.EqualTo(new Vector3(10, 0, 0)));
    }

    [Test]
    public void DetachingFromTheBoneKeepsTheParentAndDropsTheSocket()
    {
        var child = SpawnSocketedChild(Vector3.Zero);
        _world.Update(0f);

        _feature.DetachFromBone(child);
        _world.Update(0f);

        Assert.That(_world.Has<AttachSocket>(child), Is.False);
        Assert.That(_world.GetParent(child), Is.EqualTo(_parent));
        Assert.That(WorldPosition(child), Is.EqualTo(new Vector3(10, 0, 0)));
    }

    private Entity SpawnSocketedChild(Vector3 localPosition)
    {
        var child = _world.Spawn();
        _world.SetPosition(child, localPosition);
        _feature.AttachToBone(child, _parent, TestSkeletons.SocketBone);
        return child;
    }

    private Vector3 WorldPosition(Entity entity) => _world.Get<WorldTransform>(entity).Matrix.Translation;
}
