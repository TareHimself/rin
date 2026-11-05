using System.Numerics;
using Rin.World.Graphics;
using Rin.World.Transforms;

namespace Rin.World.Tests.Graphics;

public class RenderFeatureRestoreTests
{
    private World _world = null!;
    private FakeRenderSystem _render = null!;
    private FakeMeshFactory _meshes = null!;
    private WorldSnapshot _snapshot = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new World();
        _render = new FakeRenderSystem();
        _meshes = new FakeMeshFactory();
        _ = new RenderFeature(_world, _render, _meshes);
        _snapshot = new WorldSnapshot();
    }

    [TearDown]
    public void TearDown()
    {
        _render.Dispose();
        _meshes.Dispose();
    }

    [Test]
    public void RestoreDestroysEveryProxyAndRecreatesThemAtTheRestoredPose()
    {
        var mesh = SpawnMesh(new Vector3(1, 0, 0));
        var light = _world.Spawn();
        _world.SetPosition(light, new Vector3(0, 4, 0));
        _world.Add(light, new PointLight());
        Frame();
        _world.SaveSnapshot(_snapshot);
        var oldMeshProxy = _world.Get<RenderProxy>(mesh).Handle;
        _world.SetPosition(mesh, new Vector3(9, 0, 0));
        Frame();

        _world.RestoreSnapshot(_snapshot);

        Assert.That(_render.Destroyed, Does.Contain(oldMeshProxy));
        Assert.That(_render.LiveCount, Is.EqualTo(2));
        var (handle, desc) = _render.CreatedStaticMeshes[^1];
        Assert.That(desc.Transform.Translation, Is.EqualTo(new Vector3(1, 0, 0)));
        Assert.That(_world.Get<RenderProxy>(mesh).Handle, Is.EqualTo(handle));
        Assert.That(_render.CreatedLights[^1].Light.Location, Is.EqualTo(new Vector3(0, 4, 0)));
        Assert.That(_world.Get<RenderProxy>(light).Handle, Is.EqualTo(_render.CreatedLights[^1].Handle));
    }

    [Test]
    public void ProxiesFollowTheRestoredEntities()
    {
        var kept = SpawnMesh(Vector3.Zero);
        _world.SaveSnapshot(_snapshot);
        _world.Despawn(kept);
        var spawned = SpawnMesh(Vector3.One);

        _world.RestoreSnapshot(_snapshot);

        Assert.That(_world.IsAlive(spawned), Is.False);
        Assert.That(_render.LiveCount, Is.EqualTo(1));
        Assert.That(_world.Get<RenderProxy>(kept).Handle, Is.EqualTo(_render.CreatedStaticMeshes[^1].Handle));
    }

    [Test]
    public void ExtractPushesMovesOfRestoredProxies()
    {
        var mesh = SpawnMesh(Vector3.Zero);
        Frame();
        _world.SaveSnapshot(_snapshot);
        _world.RestoreSnapshot(_snapshot);
        _render.PushedTransforms.Clear();

        _world.SetPosition(mesh, new Vector3(0, 0, 3));
        Frame();

        Assert.That(_render.PushedTransforms, Has.Count.EqualTo(1));
        Assert.That(_render.PushedTransforms[0].Handle, Is.EqualTo(_world.Get<RenderProxy>(mesh).Handle));
    }

    [Test]
    public void ProxyHandlesAreNotPartOfTheSnapshot()
    {
        SpawnMesh(Vector3.Zero);
        SpawnMesh(Vector3.One);
        Frame();
        _world.SaveSnapshot(_snapshot);
        var resaved = new WorldSnapshot();

        _world.RestoreSnapshot(_snapshot);
        _world.SaveSnapshot(resaved);

        Assert.That(_render.CreatedStaticMeshes, Has.Count.EqualTo(4), "the restore created new handles");
        Assert.That(resaved.Bytes.ToArray(), Is.EqualTo(_snapshot.Bytes.ToArray()));
    }

    private Entity SpawnMesh(Vector3 position)
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, position);
        _world.Add(entity, new StaticMesh { MeshId = _meshes.Register(new FakeMesh()) });
        return entity;
    }

    private void Frame() => _world.Update(0f);
}
