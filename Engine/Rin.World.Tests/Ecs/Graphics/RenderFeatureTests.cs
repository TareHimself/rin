using System.Numerics;
using Rin.World.Graphics.Mesh;
using Rin.World.Ecs;
using Rin.World.Ecs.Graphics;
using Rin.World.Ecs.Transforms;
using Rin.World.Graphics;

namespace Rin.World.Tests.Ecs.Graphics;

public class RenderFeatureTests
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
    public void AddingAStaticMeshCreatesAProxyAndARenderProxyComponent()
    {
        var entity = _world.Spawn();

        _world.Add(entity, new StaticMesh { MeshId = _meshes.Register(new FakeMesh()) });

        Assert.That(_render.CreatedStaticMeshes, Has.Count.EqualTo(1));
        Assert.That(_world.Get<RenderProxy>(entity).Handle, Is.EqualTo(_render.CreatedStaticMeshes[0].Handle));
    }

    [Test]
    public void AddingAStaticMeshThroughTheCommandBufferCreatesTheProxyOnPlayback()
    {
        var entity = _world.Spawn();
        _world.Commands.Add(entity, new StaticMesh { MeshId = _meshes.Register(new FakeMesh()) });

        Assert.That(_render.CreatedStaticMeshes, Is.Empty);

        _world.Commands.Playback();

        Assert.That(_render.CreatedStaticMeshes, Has.Count.EqualTo(1));
        Assert.That(_world.Has<RenderProxy>(entity), Is.True);
    }

    [Test]
    public void AStaticMeshAddedFromInsideAQueryLoopThroughCommandsGetsAProxy()
    {
        var meshId = _meshes.Register(new FakeMesh());
        var entity = _world.Spawn();
        _world.Add(entity, new Health(1));

        foreach (var (_, entities) in _world.Query<Health>())
            for (var i = 0; i < entities.Length; i++)
                _world.Commands.Add(entities[i], new StaticMesh { MeshId = meshId });
        _world.Update(0f);

        Assert.That(_world.Has<RenderProxy>(entity), Is.True);
    }

    [Test]
    public void AMeshThatIsNotReadyGetsNoProxy()
    {
        var entity = _world.Spawn();

        _world.Add(entity, new StaticMesh { MeshId = 42 });

        Assert.That(_render.CreatedStaticMeshes, Is.Empty);
        Assert.That(_world.Has<RenderProxy>(entity), Is.False);
    }

    [Test]
    public void TheProxyStartsAtTheCurrentWorldPoseBeforePropagationRuns()
    {
        var parent = _world.Spawn();
        var child = _world.Spawn();
        _world.SetPosition(parent, new Vector3(10, 0, 0));
        _world.SetPosition(child, new Vector3(0, 2, 0));
        _world.SetParent(child, parent);

        _world.Add(child, new StaticMesh { MeshId = _meshes.Register(new FakeMesh()) });

        Assert.That(_render.CreatedStaticMeshes[0].Desc.Transform.Translation, Is.EqualTo(new Vector3(10, 2, 0)));
    }

    [Test]
    public void AnEntityWithoutTransformsGetsAnIdentityProxy()
    {
        var entity = _world.Spawn();

        _world.Add(entity, new StaticMesh { MeshId = _meshes.Register(new FakeMesh()) });

        Assert.That(_render.CreatedStaticMeshes[0].Desc.Transform, Is.EqualTo(Matrix4x4.Identity));
    }

    [Test]
    public void SurfacesUseTheirMaterialAndFallBackToThePreviousOne()
    {
        var first = new FakeMeshMaterial();
        var third = new FakeMeshMaterial();
        var mesh = new FakeMesh { Surfaces = new MeshSurface[4] };
        var entity = _world.Spawn();

        _world.Add(entity, new StaticMesh
        {
            MeshId = _meshes.Register(mesh),
            Materials = _feature.Materials.Add(first, null, third)
        });

        var desc = _render.CreatedStaticMeshes[0].Desc;
        Assert.That(desc.SurfaceIndices, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(desc.Materials, Is.EqualTo(new IMeshMaterial[] { first, first, third, third }));
    }

    [Test]
    public void RemovingTheStaticMeshDestroysTheProxyAndRemovesRenderProxy()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new StaticMesh { MeshId = _meshes.Register(new FakeMesh()) });
        var handle = _world.Get<RenderProxy>(entity).Handle;

        _world.Remove<StaticMesh>(entity);

        Assert.That(_render.Destroyed, Is.EqualTo(new[] { handle }));
        Assert.That(_world.Has<RenderProxy>(entity), Is.False);
    }

    [Test]
    public void RemovingThroughTheCommandBufferDestroysTheProxyOnPlayback()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new PointLight());
        _world.Commands.Remove<PointLight>(entity);

        _world.Commands.Playback();

        Assert.That(_render.DestroyCount, Is.EqualTo(1));
        Assert.That(_world.Has<RenderProxy>(entity), Is.False);
    }

    [Test]
    public void DespawningAParentDestroysTheProxiesOfItsChildren()
    {
        var meshId = _meshes.Register(new FakeMesh());
        var parent = _world.Spawn();
        var child = _world.Spawn();
        var grandchild = _world.Spawn();
        _world.SetParent(child, parent);
        _world.SetParent(grandchild, child);
        _world.Add(parent, new StaticMesh { MeshId = meshId });
        _world.Add(child, new PointLight());
        _world.Add(grandchild, new DirectionalLight());

        _world.Despawn(parent);

        Assert.That(_render.DestroyCount, Is.EqualTo(3));
        Assert.That(_render.LiveCount, Is.Zero);
    }

    [Test]
    public void DespawningThroughTheCommandBufferDestroysTheProxy()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new StaticMesh { MeshId = _meshes.Register(new FakeMesh()) });
        _world.Commands.Despawn(entity);

        _world.Update(0f);

        Assert.That(_render.LiveCount, Is.Zero);
    }

    [Test]
    public void ASecondRenderComponentOnOneEntityThrows()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new PointLight());

        Assert.Throws<InvalidOperationException>(() => _world.Add(entity, new DirectionalLight()));
    }

    [Test]
    public void AddingALightCreatesALightProxyFromItsProperties()
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, new Vector3(1, 2, 3));

        _world.Add(entity, new PointLight { Radiance = 50f, Radius = 7f });

        var light = _render.CreatedLights.Single().Light;
        Assert.That(light.LightType, Is.EqualTo(LightType.Point));
        Assert.That(light.Location, Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(light.Radiance, Is.EqualTo(50f));
        Assert.That(light.Radius, Is.EqualTo(7f));
        Assert.That(_world.Get<PointLight>(entity).IsDirty, Is.False);
    }
}
