using System.Numerics;
using Rin.World.Ecs;
using Rin.World.Ecs.Physics;
using Rin.World.Ecs.Transforms;
using Rin.World.Physics;

namespace Rin.World.Tests.Ecs.Physics;

public class PhysicsFeatureTests
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

    private static RigidBody Box(PhysicsState state = PhysicsState.Simulated) =>
        new() { Shape = ColliderShape.Box(new Vector3(2f)), State = state };

    [Test]
    public void AddingARigidBodyCreatesABodyAndMapsItToTheEntity()
    {
        var entity = _world.Spawn();

        _world.Add(entity, Box());

        Assert.That(_physics.BodyCount, Is.EqualTo(1));
        var body = _world.Get<PhysicsBody>(entity);
        Assert.That(body.Handle, Is.EqualTo(_physics.LastCreated));
        Assert.That(body.State, Is.EqualTo(PhysicsState.Simulated));
        Assert.That(_feature.FindEntity(body.Handle), Is.EqualTo(entity));
    }

    [Test]
    public void EachShapeCreatesTheMatchingBody()
    {
        var box = _world.Spawn();
        var sphere = _world.Spawn();
        var capsule = _world.Spawn();

        _world.Add(box, new RigidBody { Shape = ColliderShape.Box(new Vector3(1, 2, 3)) });
        _world.Add(sphere, new RigidBody { Shape = ColliderShape.Sphere(0.5f) });
        _world.Add(capsule, new RigidBody { Shape = ColliderShape.Capsule(0.25f, 1f) });

        Assert.That(_physics.GetBoxSize(_world.Get<PhysicsBody>(box).Handle), Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(_physics.GetSphereRadius(_world.Get<PhysicsBody>(sphere).Handle), Is.EqualTo(0.5f));
        Assert.That(_physics.GetCapsuleRadius(_world.Get<PhysicsBody>(capsule).Handle), Is.EqualTo(0.25f));
        Assert.That(_physics.GetCapsuleHalfHeight(_world.Get<PhysicsBody>(capsule).Handle), Is.EqualTo(1f));
    }

    [Test]
    public void TheBodyGetsMassTriggerAndChannelFromTheRigidBody()
    {
        var entity = _world.Spawn();

        _world.Add(entity, Box() with { Mass = 4f, IsTrigger = true, CollisionChannel = 3 });

        var handle = _world.Get<PhysicsBody>(entity).Handle;
        Assert.That(_physics.GetMass(handle), Is.EqualTo(4f));
        Assert.That(_physics.GetTrigger(handle), Is.True);
        Assert.That(_physics.GetCollisionChannel(handle), Is.EqualTo(3));
    }

    [Test]
    public void AChildBodyStartsAtTheWorldPoseFromTheLocalChainBeforePropagation()
    {
        var parent = _world.Spawn();
        var child = _world.Spawn();
        _world.SetPosition(parent, new Vector3(10, 0, 0));
        _world.SetRotation(parent, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2));
        _world.SetPosition(child, new Vector3(1, 0, 0));
        _world.SetParent(child, parent);

        _world.Add(child, Box());

        var position = _physics.GetPosition(_world.Get<PhysicsBody>(child).Handle);
        Assert.That(Vector3.Distance(position, new Vector3(10, 0, -1)), Is.LessThan(1e-5f));
    }

    [Test]
    public void AVelocityPresentAtCreationIsAppliedToTheNewBody()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new Velocity { Linear = new Vector3(1, 2, 3), Angular = new Vector3(0, 1, 0) });

        _world.Add(entity, Box());

        var handle = _world.Get<PhysicsBody>(entity).Handle;
        Assert.That(_physics.GetLinearVelocity(handle), Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(_physics.GetAngularVelocity(handle), Is.EqualTo(new Vector3(0, 1, 0)));
        Assert.That(_world.Get<Velocity>(entity).IsDirty, Is.False);
    }

    [Test]
    public void RemovingTheRigidBodyDestroysTheBodyAndUnmapsIt()
    {
        var entity = _world.Spawn();
        _world.Add(entity, Box());
        var handle = _world.Get<PhysicsBody>(entity).Handle;

        _world.Remove<RigidBody>(entity);

        Assert.That(_physics.BodyCount, Is.Zero);
        Assert.That(_world.Has<PhysicsBody>(entity), Is.False);
        Assert.That(_feature.FindEntity(handle).IsValid, Is.False);
    }

    [Test]
    public void ARigidBodyAddedThroughTheCommandBufferGetsItsBodyOnPlayback()
    {
        var entity = _world.Spawn();
        _world.Commands.Add(entity, Box());

        Assert.That(_physics.BodyCount, Is.Zero);

        _world.Commands.Playback();

        Assert.That(_physics.BodyCount, Is.EqualTo(1));
        Assert.That(_world.Has<PhysicsBody>(entity), Is.True);
    }

    [Test]
    public void DespawningAParentDestroysTheBodiesOfItsChildrenToo()
    {
        var parent = _world.Spawn();
        var child = _world.Spawn();
        _world.SetParent(child, parent);
        _world.Add(parent, Box(PhysicsState.Controlled));
        _world.Add(child, Box(PhysicsState.Controlled));
        var childHandle = _world.Get<PhysicsBody>(child).Handle;

        _world.Commands.Despawn(parent);
        _world.Commands.Playback();

        Assert.That(_physics.BodyCount, Is.Zero);
        Assert.That(_feature.FindEntity(childHandle).IsValid, Is.False);
    }

    [Test]
    public void ABodyCreatedDirectlyOnThePhysicsSystemHasNoEntity()
    {
        var handle = _physics.CreateSphere(1f, new Rin.Core.Shared.Math.Transform(), PhysicsState.Static);

        Assert.That(_feature.FindEntity(handle).IsValid, Is.False);
    }
}
