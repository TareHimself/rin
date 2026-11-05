using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Ecs;
using Rin.World.Ecs.Physics;
using Rin.World.Ecs.Scheduling;
using Rin.World.Ecs.Transforms;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests.Ecs.Physics;

public class ContactDispatchTests
{
    private EntityWorld _world = null!;
    private FakePhysicsSystem _physics = null!;
    private PhysicsFeature _feature = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new EntityWorld();
        _physics = new FakePhysicsSystem();
        _feature = new PhysicsFeature(_world, _physics);
    }

    private Entity SpawnBody()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new RigidBody { Shape = ColliderShape.Sphere(1f), State = PhysicsState.Simulated });
        return entity;
    }

    private PhysicsBodyHandle HandleOf(Entity entity) => _world.Get<PhysicsBody>(entity).Handle;

    private void Tick() => _world.Update(_world.TickInterval);

    [Test]
    public void ContactEventsAreMappedToTheirEntities()
    {
        var a = SpawnBody();
        var b = SpawnBody();
        _physics.EmitContact(HandleOf(a), HandleOf(b), PhysicsContactKind.Begin, true);

        Tick();

        Assert.That(_feature.Contacts.ToArray(), Is.EqualTo(new[] { new EntityContact(a, b, PhysicsContactKind.Begin, true) }));
        Assert.That(_feature.Contacts[0].Other(a), Is.EqualTo(b));
    }

    [Test]
    public void TheNextTicksGameplaySeesTheContactsOfTheTickBefore()
    {
        var a = SpawnBody();
        var b = SpawnBody();
        var recorder = new ContactRecorder(_feature);
        _world.AddSystem(TickPhase.Gameplay, recorder);

        Tick();
        _physics.EmitContact(HandleOf(a), HandleOf(b), PhysicsContactKind.Begin, false);
        Tick();
        Tick();
        Tick();

        Assert.That(recorder.SeenPerTick, Is.EqualTo(new[] { 0, 0, 1, 0 }));
    }

    [Test]
    public void ABodyWithoutAnEntityShowsUpAsADefaultEntity()
    {
        var a = SpawnBody();
        var loose = _physics.CreateSphere(1f, new Transform(), PhysicsState.Static);
        var otherLoose = _physics.CreateSphere(1f, new Transform(), PhysicsState.Static);
        _physics.EmitContact(HandleOf(a), loose, PhysicsContactKind.End, false);
        _physics.EmitContact(loose, otherLoose, PhysicsContactKind.Begin, false);

        Tick();

        Assert.That(_feature.Contacts.ToArray(), Is.EqualTo(new[] { new EntityContact(a, default, PhysicsContactKind.End, false) }));
    }

    [Test]
    public void MoreContactsThanTheInitialBufferAreAllKept()
    {
        var entities = new Entity[40];
        for (var i = 0; i < entities.Length; i++) entities[i] = SpawnBody();
        for (var i = 1; i < entities.Length; i++)
            _physics.EmitContact(HandleOf(entities[0]), HandleOf(entities[i]), PhysicsContactKind.Begin, false);

        Tick();

        Assert.That(_feature.Contacts.Length, Is.EqualTo(entities.Length - 1));
        Assert.That(_feature.Contacts[^1].B, Is.EqualTo(entities[^1]));
    }

    [Test]
    public void ARealLandingIsReportedForTheFallingEntity()
    {
        var world = new EntityWorld();
        var physics = new BepuPhysicsSystem();
        try
        {
            var feature = new PhysicsFeature(world, physics);
            var floor = world.Spawn();
            world.Add(floor, new RigidBody { Shape = ColliderShape.Box(new Vector3(10, 1, 10)), State = PhysicsState.Static });
            var ball = world.Spawn();
            world.SetPosition(ball, new Vector3(0, 2, 0));
            world.Add(ball, new RigidBody { Shape = ColliderShape.Sphere(0.5f), State = PhysicsState.Simulated });

            var contacts = new List<EntityContact>();
            for (var i = 0; i < 120; i++)
            {
                world.Update(world.TickInterval);
                contacts.AddRange(feature.Contacts.ToArray());
            }

            Assert.That(contacts, Has.Count.EqualTo(1));
            Assert.That(contacts[0].Kind, Is.EqualTo(PhysicsContactKind.Begin));
            Assert.That(contacts[0].Other(ball), Is.EqualTo(floor));
        }
        finally
        {
            physics.Destroy();
        }
    }

    private sealed class ContactRecorder(PhysicsFeature feature) : TickSystem
    {
        public List<int> SeenPerTick { get; } = [];

        public override void Tick(in TickContext context) => SeenPerTick.Add(feature.Contacts.Length);
    }
}
