using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;
using Rin.World.Transforms;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests.Physics;

public class PhysicsFeatureQueryTests
{
    private World _world = null!;
    private BepuPhysicsSystem _physics = null!;
    private PhysicsFeature _feature = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new World();
        _physics = new BepuPhysicsSystem();
        _feature = new PhysicsFeature(_world, _physics);
    }

    [TearDown]
    public void TearDown() => _physics.Destroy();

    private Entity StaticSphereAt(Vector3 position)
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, position);
        _world.Add(entity, new RigidBody { Shape = ColliderShape.Sphere(1f), State = PhysicsState.Static });
        return entity;
    }

    [Test]
    public void RayCastReturnsTheEntityOfTheClosestHit()
    {
        var near = StaticSphereAt(new Vector3(0, 0, 5));
        StaticSphereAt(new Vector3(0, 0, 10));

        var hit = _feature.RayCast(Vector3.Zero, Vector3.UnitZ, 20f);

        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Value.Entity, Is.EqualTo(near));
        Assert.That(hit.Value.Hit.Distance, Is.EqualTo(4f).Within(0.01f));
    }

    [Test]
    public void RayCastMissReturnsNull()
    {
        StaticSphereAt(new Vector3(10, 0, 0));

        Assert.That(_feature.RayCast(Vector3.Zero, Vector3.UnitZ, 20f), Is.Null);
    }

    [Test]
    public void RayCastAllFillsTheSpanWithEveryPiercedEntity()
    {
        var a = StaticSphereAt(new Vector3(0, 0, 5));
        var b = StaticSphereAt(new Vector3(0, 0, 10));

        Span<EntityHit> hits = stackalloc EntityHit[8];
        var count = _feature.RayCastAll(Vector3.Zero, Vector3.UnitZ, 20f, hits);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(new[] { hits[0].Entity, hits[1].Entity }, Is.EquivalentTo(new[] { a, b }));
    }

    [Test]
    public void SphereCastAllStopsWhenTheBufferIsFull()
    {
        StaticSphereAt(new Vector3(0, 0, 5));
        StaticSphereAt(new Vector3(0, 0, 10));

        var hits = new EntityHit[1];
        var count = _feature.SphereCastAll(0.5f, Vector3.Zero, Vector3.UnitZ, 20f, hits);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(hits[0].Entity.IsValid, Is.True);
    }

    [Test]
    public void AHitOnABodyWithoutAnEntityKeepsTheBodyButNoEntity()
    {
        var loose = _physics.CreateSphere(1f, new Transform { Position = new Vector3(0, 0, 5) }, PhysicsState.Static);

        var hit = _feature.SphereCast(0.5f, Vector3.Zero, Vector3.UnitZ, 20f);

        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Value.Entity.IsValid, Is.False);
        Assert.That(hit.Value.Hit.Body, Is.EqualTo(loose));
    }

    [Test]
    public void OverlapSphereReturnsTheOverlappingEntitiesAndSkipsBodiesWithoutOne()
    {
        var inside = StaticSphereAt(new Vector3(1, 0, 0));
        StaticSphereAt(new Vector3(20, 0, 0));
        _physics.CreateSphere(1f, new Transform { Position = new Vector3(-1, 0, 0) }, PhysicsState.Static);

        Span<Entity> entities = stackalloc Entity[8];
        var count = _feature.OverlapSphere(1f, Vector3.Zero, entities);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(entities[0], Is.EqualTo(inside));
    }

    [Test]
    public void OverlapBoxAndCapsuleMapToEntities()
    {
        var inside = StaticSphereAt(new Vector3(1, 0, 0));

        var entities = new Entity[4];
        var boxCount = _feature.OverlapBox(new Vector3(2f), Vector3.Zero, Quaternion.Identity, entities);
        var boxEntity = entities[0];
        var capsuleCount = _feature.OverlapCapsule(0.5f, 1f, Vector3.Zero, Quaternion.Identity, entities);

        Assert.That((boxCount, boxEntity), Is.EqualTo((1, inside)));
        Assert.That((capsuleCount, entities[0]), Is.EqualTo((1, inside)));
    }

    [Test]
    public void TheQueryHitsABodyAtItsSimulatedPositionAfterTicks()
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, new Vector3(0, 5, 0));
        _world.Add(entity, new RigidBody { Shape = ColliderShape.Sphere(0.5f), State = PhysicsState.Simulated });

        for (var i = 0; i < 30; i++) _world.Update(_world.TickInterval);

        var y = _world.Get<LocalTransform>(entity).Position.Y;
        var hit = _feature.RayCast(new Vector3(-5, y, 0), Vector3.UnitX, 10f);
        Assert.That(y, Is.LessThan(5f));
        Assert.That(hit?.Entity, Is.EqualTo(entity));
    }
}
