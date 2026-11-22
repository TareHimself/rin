using System.Numerics;
using Rin.World.Physics;
using Rin.World.Transforms;

namespace Rin.World.Tests.Physics;

public class PhysicsFeatureTests : PhysicsFeatureFixture
{
    private void AddBox(Entity entity, RigidBody body) => AddBox(entity, body, new Vector3(2f));

    private void AddBox(Entity entity, PhysicsState state = PhysicsState.Simulated) =>
        AddBox(entity, new RigidBody { State = state });

    private void AddBox(Entity entity, RigidBody body, Vector3 size)
    {
        _world.Add(entity, body);
        _world.Add(entity, new BoxCollider { Size = size });
    }

    [Test]
    public void AddingARigidBodyCreatesABodyAndMapsItToTheEntity()
    {
        var entity = _world.Spawn();

        AddBox(entity);

        Assert.That(_physics.BodyCount, Is.EqualTo(1));
        var body = _world.Get<PhysicsBody>(entity);
        Assert.That(body.Handle, Is.EqualTo(_physics.LastCreated));
        Assert.That(body.State, Is.EqualTo(PhysicsState.Simulated));
        Assert.That(_feature.FindEntity(body.Handle), Is.EqualTo(entity));
    }

    [Test]
    public void TheBodyIsBuiltOnceTheEntityHasABodyAndAColliderInEitherOrder()
    {
        var bodyFirst = _world.Spawn();
        var colliderFirst = _world.Spawn();

        _world.Add(bodyFirst, new RigidBody());
        var countWithoutCollider = _physics.BodyCount;
        _world.Add(bodyFirst, new SphereCollider());
        _world.Add(colliderFirst, new SphereCollider());
        _world.Add(colliderFirst, new RigidBody());

        Assert.That(countWithoutCollider, Is.Zero);
        Assert.That(_physics.BodyCount, Is.EqualTo(2));
    }

    [Test]
    public void RemovingTheColliderDestroysTheBody()
    {
        var entity = _world.Spawn();
        AddBox(entity);

        _world.Remove<BoxCollider>(entity);

        Assert.That(_physics.BodyCount, Is.Zero);
        Assert.That(_world.Has<PhysicsBody>(entity), Is.False);
    }

    [Test]
    public void ABodyWithTwoCollidersThrows()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new SphereCollider());
        _world.Add(entity, new BoxCollider());

        Assert.Throws<InvalidOperationException>(() => _world.Add(entity, new RigidBody()));
    }

    [Test]
    public void SettingAColliderReplacesTheOneTheEntityHad()
    {
        var entity = _world.Spawn();
        AddBox(entity);

        Colliders.Set(_world, entity, ColliderShape.Sphere(0.5f));

        Assert.That(_world.Has<BoxCollider>(entity), Is.False);
        Assert.That(_world.Get<SphereCollider>(entity).Radius, Is.EqualTo(0.5f));
        Assert.That(_physics.BodyCount, Is.EqualTo(1));
    }

    [Test]
    public void EachShapeCreatesTheMatchingBody()
    {
        var box = _world.Spawn();
        var sphere = _world.Spawn();
        var capsule = _world.Spawn();

        _world.Add(box, new RigidBody());
        _world.Add(box, new BoxCollider { Size = new Vector3(1, 2, 3) });
        _world.Add(sphere, new RigidBody());
        _world.Add(sphere, new SphereCollider { Radius = 0.5f });
        _world.Add(capsule, new RigidBody());
        _world.Add(capsule, new CapsuleCollider { Radius = 0.25f, HalfHeight = 1f });

        Assert.That(_physics.GetBoxSize(_world.Get<PhysicsBody>(box).Handle), Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(_physics.GetSphereRadius(_world.Get<PhysicsBody>(sphere).Handle), Is.EqualTo(0.5f));
        Assert.That(_physics.GetCapsuleRadius(_world.Get<PhysicsBody>(capsule).Handle), Is.EqualTo(0.25f));
        Assert.That(_physics.GetCapsuleHalfHeight(_world.Get<PhysicsBody>(capsule).Handle), Is.EqualTo(1f));
    }

    [Test]
    public void ACompoundShapeCreatesOneBodyFromItsRegisteredParts()
    {
        var parts = new[]
        {
            new ShapePart(ColliderShape.Box(new Vector3(1f)), new Vector3(-1f, 0f, 0f)),
            new ShapePart(ColliderShape.Sphere(0.5f), new Vector3(1f, 0f, 0f))
        };
        var entity = _world.Spawn();

        _world.Add(entity, new RigidBody());
        _world.Add(entity, new CompoundCollider { Parts = _feature.Shapes.AddCompound(parts) });

        Assert.That(_physics.GetCompoundParts(_world.Get<PhysicsBody>(entity).Handle), Is.EqualTo(parts));
    }

    [Test]
    public void TheBodyGetsMassTriggerAndChannelFromTheRigidBody()
    {
        var entity = _world.Spawn();

        AddBox(entity, new RigidBody { Mass = 4f, IsTrigger = true, CollisionChannel = 3 });

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

        AddBox(child);

        var position = _physics.GetPosition(_world.Get<PhysicsBody>(child).Handle);
        Assert.That(Vector3.Distance(position, new Vector3(10, 0, -1)), Is.LessThan(1e-5f));
    }

    [Test]
    public void AVelocityPresentAtCreationIsAppliedToTheNewBody()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new Velocity { Linear = new Vector3(1, 2, 3), Angular = new Vector3(0, 1, 0) });

        AddBox(entity);

        var handle = _world.Get<PhysicsBody>(entity).Handle;
        Assert.That(_physics.GetLinearVelocity(handle), Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(_physics.GetAngularVelocity(handle), Is.EqualTo(new Vector3(0, 1, 0)));
    }

    [Test]
    public void RemovingTheRigidBodyDestroysTheBodyAndUnmapsIt()
    {
        var entity = _world.Spawn();
        AddBox(entity);
        var handle = _world.Get<PhysicsBody>(entity).Handle;

        _world.Remove<RigidBody>(entity);

        Assert.That(_physics.BodyCount, Is.Zero);
        Assert.That(_world.Has<PhysicsBody>(entity), Is.False);
        Assert.That(_feature.FindEntity(handle).IsValid, Is.False);
    }

    [Test]
    public void DespawningAParentDestroysTheBodiesOfItsChildrenToo()
    {
        var parent = _world.Spawn();
        var child = _world.Spawn();
        _world.SetParent(child, parent);
        AddBox(parent, PhysicsState.Controlled);
        AddBox(child, PhysicsState.Controlled);
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
