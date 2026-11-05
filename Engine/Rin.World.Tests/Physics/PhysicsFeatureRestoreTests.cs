using System.Numerics;
using Rin.World.Physics;
using Rin.World.Transforms;

namespace Rin.World.Tests.Physics;

public class PhysicsFeatureRestoreTests
{
    private World _world = null!;
    private FakePhysicsSystem _physics = null!;
    private PhysicsFeature _feature = null!;
    private WorldSnapshot _snapshot = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new World();
        _physics = new FakePhysicsSystem();
        _feature = new PhysicsFeature(_world, _physics);
        _snapshot = new WorldSnapshot();
    }

    [Test]
    public void RestoreRecreatesBodiesFromTheRestoredPoseAndVelocity()
    {
        var entity = SpawnBody(new Vector3(0, 10, 0));
        _world.Add(entity, new Velocity { Linear = new Vector3(1, 0, 0) });
        Tick();
        _world.SaveSnapshot(_snapshot);
        var oldBody = BodyOf(entity);
        _physics.OnUpdate = _ => _physics.SetPosition(BodyOf(entity), new Vector3(0, -50, 0));
        Tick();
        _physics.OnUpdate = null;

        _world.RestoreSnapshot(_snapshot);

        var body = BodyOf(entity);
        Assert.That(body, Is.Not.EqualTo(oldBody));
        Assert.That(_physics.BodyCount, Is.EqualTo(1));
        Assert.That(_physics.GetPosition(body), Is.EqualTo(new Vector3(0, 10, 0)));
        Assert.That(_physics.GetLinearVelocity(body), Is.EqualTo(new Vector3(1, 0, 0)));
        Assert.That(_world.Get<PhysicsBody>(entity).State, Is.EqualTo(PhysicsState.Simulated));
        Assert.That(_feature.FindEntity(body), Is.EqualTo(entity));
        Assert.That(_feature.FindEntity(oldBody), Is.EqualTo(default(Entity)));
    }

    [Test]
    public void BodiesFollowTheRestoredEntities()
    {
        var kept = SpawnBody(Vector3.Zero);
        _world.SaveSnapshot(_snapshot);
        _world.Despawn(kept);
        SpawnBody(Vector3.One);
        SpawnBody(Vector3.One);

        _world.RestoreSnapshot(_snapshot);

        Assert.That(_physics.BodyCount, Is.EqualTo(1));
        Assert.That(_feature.FindEntity(BodyOf(kept)), Is.EqualTo(kept));
    }

    [Test]
    public void RestoreClearsTheContacts()
    {
        var a = SpawnBody(Vector3.Zero);
        var b = SpawnBody(Vector3.One);
        _world.SaveSnapshot(_snapshot);
        _physics.EmitContact(BodyOf(a), BodyOf(b), PhysicsContactKind.Begin, false);
        Tick();
        Assert.That(_feature.Contacts.Length, Is.EqualTo(1));

        _world.RestoreSnapshot(_snapshot);

        Assert.That(_feature.Contacts.Length, Is.Zero);
    }

    [Test]
    public void BodyHandlesAreNotPartOfTheSnapshot()
    {
        var entity = SpawnBody(Vector3.Zero);
        SpawnBody(Vector3.One);
        _world.SaveSnapshot(_snapshot);
        var oldBody = BodyOf(entity);
        var resaved = new WorldSnapshot();

        _world.RestoreSnapshot(_snapshot);
        _world.SaveSnapshot(resaved);

        Assert.That(BodyOf(entity), Is.Not.EqualTo(oldBody));
        Assert.That(resaved.Bytes.ToArray(), Is.EqualTo(_snapshot.Bytes.ToArray()));
    }

    private Entity SpawnBody(Vector3 position)
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, position);
        _world.Add(entity, new RigidBody { Shape = ColliderShape.Sphere(0.5f), State = PhysicsState.Simulated });
        return entity;
    }

    private PhysicsBodyHandle BodyOf(Entity entity) => _world.Get<PhysicsBody>(entity).Handle;

    private void Tick() => _world.Update(_world.TickInterval);
}
