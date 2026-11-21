using System.Numerics;
using Rin.Core.Graphics;
using Rin.Graphics.Null;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Transforms;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

public class EnvironmentLightTests : RenderFeatureFixture
{
    private EnvironmentMapHandle _map;

    [SetUp]
    public void SetUp()
    {
        _map = _feature.Environments.Create(TestEnvironments.Map(Vector3.One), new NullGraphicsModule());
    }

    private Entity SpawnLight()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new EnvironmentLight { Map = _map, Intensity = 2f });
        return entity;
    }

    [Test]
    public void AddingALightCreatesAProxyForItsMap()
    {
        var entity = SpawnLight();

        Assert.Multiple(() =>
        {
            Assert.That(_render.CreatedEnvironments, Has.Count.EqualTo(1));
            Assert.That(_render.CreatedEnvironments[0].Volume.Map, Is.SameAs(_feature.Environments.Get(_map)));
            Assert.That(_render.CreatedEnvironments[0].Volume.Intensity, Is.EqualTo(2f));
            Assert.That(_world.Get<RenderProxy>(entity).Handle, Is.EqualTo(_render.CreatedEnvironments[0].Handle));
        });
    }

    [Test]
    public void AddingALightWithoutALiveMapThrows()
    {
        var entity = _world.Spawn();

        Assert.Throws<InvalidOperationException>(() => _world.Add(entity, new EnvironmentLight()));
    }

    [Test]
    public void RemovingTheLightDestroysItsProxy()
    {
        var entity = SpawnLight();

        _world.Remove<EnvironmentLight>(entity);

        Assert.Multiple(() =>
        {
            Assert.That(_render.DestroyCount, Is.EqualTo(1));
            Assert.That(_world.Has<RenderProxy>(entity), Is.False);
        });
    }

    [Test]
    public void APropertyChangeIsPushedOnce()
    {
        var entity = SpawnLight();
        _world.Update(0f);
        _render.PushedEnvironments.Clear();

        _world.Write<EnvironmentLight>(entity).Extent = new Vector3(4f);
        _world.Update(0f);
        _world.Update(0f);

        Assert.Multiple(() =>
        {
            Assert.That(_render.PushedEnvironments, Has.Count.EqualTo(1));
            Assert.That(_render.PushedEnvironments[0].Volume.Extent, Is.EqualTo(new Vector3(4f)));
        });
    }

    [Test]
    public void MovingTheEntityPushesTheNewTransform()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new LocalTransform());
        _world.Add(entity, new WorldTransform());
        _world.Add(entity, new EnvironmentLight { Map = _map, Extent = new Vector3(1f) });
        _world.Update(0f);
        _render.PushedEnvironments.Clear();

        _world.Write<LocalTransform>(entity).Position = new Vector3(5f, 0f, 0f);
        _world.Update(0f);

        Assert.Multiple(() =>
        {
            Assert.That(_render.PushedEnvironments, Has.Count.EqualTo(1));
            Assert.That(_render.PushedEnvironments[0].Volume.Transform.Translation, Is.EqualTo(new Vector3(5f, 0f, 0f)));
        });
    }

    [Test]
    public void TheDefaultRenderSystemPutsVolumesInItsSnapshotAndPicksOneForTheView()
    {
        using var render = new DefaultRenderSystem();
        var sky = TestEnvironments.Unbounded(1, Vector3.One);
        var handle = render.CreateEnvironmentProxy(sky);

        using var view = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(view.Snapshot.Environments.Length, Is.EqualTo(1));
            Assert.That(view.Environment.FallbackCubemap, Is.EqualTo(sky.Map.Cubemap));
        });

        render.DestroyProxy(handle);
        using var after = (DefaultWorldViewData)render.Snapshot(TestCameras.Identity, new Extent2D(1, 1));
        Assert.That(after.Environment.HasCubemap, Is.False);
    }
}
