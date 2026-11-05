using System.Numerics;
using Rin.Core.Views;
using Rin.World.Ecs;
using Rin.World.Ecs.Graphics;
using Rin.World.Ecs.Transforms;
using Rin.World.Graphics;

namespace Rin.World.Tests.Ecs.Graphics;

public class RenderExtractTests
{
    private EntityWorld _world = null!;
    private FakeRenderSystem _render = null!;
    private FakeMeshFactory _meshes = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new EntityWorld();
        _render = new FakeRenderSystem();
        _meshes = new FakeMeshFactory();
        _ = new RenderFeature(_world, _render, _meshes);
    }

    [TearDown]
    public void TearDown()
    {
        _render.Dispose();
        _meshes.Dispose();
    }

    [Test]
    public void AMeshTransformIsPushedOnlyWhenItsVersionChanges()
    {
        var entity = SpawnMesh(new Vector3(1, 0, 0));
        _world.Update(0f);
        _render.PushedTransforms.Clear();

        _world.Update(0f);
        Assert.That(_render.PushedTransforms, Is.Empty);

        _world.SetPosition(entity, new Vector3(5, 0, 0));
        _world.Update(0f);

        Assert.That(_render.PushedTransforms, Has.Count.EqualTo(1));
        Assert.That(_render.PushedTransforms[0].Transform.Translation, Is.EqualTo(new Vector3(5, 0, 0)));
        Assert.That(_world.Get<RenderProxy>(entity).LastPushedVersion, Is.EqualTo(_world.Get<WorldTransform>(entity).Version));
    }

    [Test]
    public void MovingAParentPushesTheChildMesh()
    {
        var parent = _world.Spawn();
        _world.SetPosition(parent, Vector3.Zero);
        var child = SpawnMesh(new Vector3(0, 1, 0));
        _world.SetParent(child, parent);
        _world.Update(0f);
        _render.PushedTransforms.Clear();

        _world.SetPosition(parent, new Vector3(3, 0, 0));
        _world.Update(0f);

        Assert.That(_render.PushedTransforms.Single().Transform.Translation, Is.EqualTo(new Vector3(3, 1, 0)));
    }

    [Test]
    public void ALightPropertyChangeIsPushed()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new PointLight());
        _world.Update(0f);
        _render.PushedLights.Clear();

        _world.Get<PointLight>(entity).Radiance = 99f;
        _world.Update(0f);
        _world.Update(0f);

        Assert.That(_render.PushedLights, Has.Count.EqualTo(1));
        Assert.That(_render.PushedLights[0].Light.Radiance, Is.EqualTo(99f));
        Assert.That(_world.Get<PointLight>(entity).IsDirty, Is.False);
    }

    [Test]
    public void ALightPropertyWrittenThroughAQuerySpanIsPushed()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new DirectionalLight());
        _world.Update(0f);
        _render.PushedLights.Clear();

        foreach (var (lights, _) in _world.Query<DirectionalLight>())
            for (var i = 0; i < lights.Length; i++)
                lights[i].Color = new Color(1f, 0f, 0f, 1f);
        _world.Update(0f);

        Assert.That(_render.PushedLights.Single().Light.Color, Is.EqualTo(new Vector3(1f, 0f, 0f)));
    }

    [Test]
    public void MovingALightPushesItsNewPosition()
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, Vector3.Zero);
        _world.Add(entity, new PointLight());
        _world.Update(0f);
        _render.PushedLights.Clear();

        _world.SetPosition(entity, new Vector3(0, 4, 0));
        _world.Update(0f);

        Assert.That(_render.PushedLights.Single().Light.Location, Is.EqualTo(new Vector3(0, 4, 0)));
    }

    [Test]
    public void ADirectionalLightPointsAlongItsWorldForwardAxis()
    {
        var entity = _world.Spawn();
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
        _world.SetRotation(entity, rotation);

        _world.Add(entity, new DirectionalLight());

        var expected = Vector3.Transform(Rin.Core.Shared.Math.MathR.Forward, rotation);
        var direction = _render.CreatedLights.Single().Light.Direction;
        Assert.That(Vector3.Distance(direction, expected), Is.LessThan(1e-5f));
    }

    [Test]
    public void TheInterpolationAlphaIsForwardedFromTheWorld()
    {
        _world.Update(_world.TickInterval * 1.5f);

        Assert.That(_render.LastInterpolationAlpha, Is.EqualTo(_world.InterpolationAlpha).Within(1e-5f));
        Assert.That(_render.LastInterpolationAlpha, Is.EqualTo(0.5f).Within(1e-3f));
    }

    [Test]
    public void ExtractingUnchangedEntitiesDoesNotAllocate()
    {
        for (var i = 0; i < 100; i++)
        {
            SpawnMesh(new Vector3(i, 0, 0));
            var light = _world.Spawn();
            _world.SetPosition(light, new Vector3(0, i, 0));
            _world.Add(light, new PointLight());
        }

        _world.Update(0f);
        _world.Update(0f);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 100; frame++) _world.Update(0f);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    private Entity SpawnMesh(Vector3 position)
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, position);
        _world.Add(entity, new StaticMesh { MeshId = _meshes.Register(new FakeMesh()) });
        return entity;
    }
}
