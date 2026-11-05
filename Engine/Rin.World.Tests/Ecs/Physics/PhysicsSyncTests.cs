using System.Numerics;
using Rin.World.Ecs;
using Rin.World.Ecs.Physics;
using Rin.World.Ecs.Transforms;
using Rin.World.Physics;

namespace Rin.World.Tests.Ecs.Physics;

public class PhysicsSyncTests
{
    private static readonly Quaternion QuarterTurnY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

    private EntityWorld _world = null!;
    private FakePhysicsSystem _physics = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new EntityWorld();
        _physics = new FakePhysicsSystem();
        _ = new PhysicsFeature(_world, _physics);
    }

    private static RigidBody Box(PhysicsState state) => new() { Shape = ColliderShape.Box(Vector3.One), State = state };

    private void Tick() => _world.Update(_world.TickInterval);

    private PhysicsBodyHandle HandleOf(Entity entity) => _world.Get<PhysicsBody>(entity).Handle;

    private Entity RotatedParentAt(Vector3 position)
    {
        var parent = _world.Spawn();
        _world.SetPosition(parent, position);
        _world.SetRotation(parent, QuarterTurnY);
        return parent;
    }

    [Test]
    public void AControlledRootBodyFollowsItsLocalTransformBeforeTheStep()
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, Vector3.Zero);
        _world.Add(entity, Box(PhysicsState.Controlled));
        var stepped = Vector3.Zero;
        _physics.OnUpdate = _ => stepped = _physics.GetPosition(HandleOf(entity));

        _world.SetPosition(entity, new Vector3(3, 4, 5));
        _world.SetRotation(entity, QuarterTurnY);
        Tick();

        Assert.That(stepped, Is.EqualTo(new Vector3(3, 4, 5)));
        Assert.That(_physics.GetOrientation(HandleOf(entity)), Is.EqualTo(QuarterTurnY));
    }

    [Test]
    public void AControlledChildBodyFollowsItsWorldPoseThroughTheParentChain()
    {
        var parent = RotatedParentAt(new Vector3(10, 0, 0));
        var child = _world.Spawn();
        _world.SetPosition(child, Vector3.Zero);
        _world.SetParent(child, parent);
        _world.Add(child, Box(PhysicsState.Controlled));

        _world.SetPosition(child, new Vector3(1, 0, 0));
        Tick();

        var handle = HandleOf(child);
        Assert.That(Vector3.Distance(_physics.GetPosition(handle), new Vector3(10, 0, -1)), Is.LessThan(1e-5f));
        Assert.That(Quaternion.Dot(_physics.GetOrientation(handle), QuarterTurnY), Is.GreaterThan(1f - 1e-5f));
    }

    [Test]
    public void AStaticBodyIsNotMovedWhenItsEntityMoves()
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, Vector3.Zero);
        _world.Add(entity, Box(PhysicsState.Static));

        _world.SetPosition(entity, new Vector3(5, 0, 0));
        Tick();

        Assert.That(_physics.GetPosition(HandleOf(entity)), Is.EqualTo(Vector3.Zero));
    }

    [Test]
    public void ASimulatedRootBodyWritesItsSteppedPoseIntoLocalTransform()
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, Vector3.Zero);
        _world.Add(entity, Box(PhysicsState.Simulated));
        _physics.OnUpdate = _ =>
        {
            _physics.SetPosition(HandleOf(entity), new Vector3(0, -1, 0));
            _physics.SetOrientation(HandleOf(entity), QuarterTurnY);
        };

        Tick();

        var local = _world.Get<LocalTransform>(entity);
        Assert.That(local.Position, Is.EqualTo(new Vector3(0, -1, 0)));
        Assert.That(local.Rotation, Is.EqualTo(QuarterTurnY));
        Assert.That(_world.Get<WorldTransform>(entity).Matrix.Translation, Is.EqualTo(new Vector3(0, -1, 0)));
    }

    [Test]
    public void ASimulatedChildBodyIsConvertedIntoItsParentsSpace()
    {
        var parent = RotatedParentAt(new Vector3(10, 0, 0));
        var child = _world.Spawn();
        _world.SetLocalTransform(child, new LocalTransform { Scale = new Vector3(3) });
        _world.SetParent(child, parent);
        _world.Add(child, Box(PhysicsState.Simulated));
        _physics.OnUpdate = _ =>
        {
            _physics.SetPosition(HandleOf(child), new Vector3(10, 0, -2));
            _physics.SetOrientation(HandleOf(child), QuarterTurnY);
        };

        Tick();

        var local = _world.Get<LocalTransform>(child);
        Assert.That(Vector3.Distance(local.Position, new Vector3(2, 0, 0)), Is.LessThan(1e-5f));
        Assert.That(Quaternion.Dot(local.Rotation, Quaternion.Identity), Is.GreaterThan(1f - 1e-5f));
        Assert.That(local.Scale, Is.EqualTo(new Vector3(3)));
        Assert.That(Vector3.Distance(_world.Get<WorldTransform>(child).Matrix.Translation, new Vector3(10, 0, -2)), Is.LessThan(1e-5f));
    }

    [Test]
    public void ASimulatedChildOfASimulatedRootConvertsAgainstTheRootsNewPose()
    {
        var root = _world.Spawn();
        var child = _world.Spawn();
        _world.SetPosition(root, Vector3.Zero);
        _world.SetPosition(child, Vector3.Zero);
        _world.SetParent(child, root);
        _world.Add(child, Box(PhysicsState.Simulated));
        _world.Add(root, Box(PhysicsState.Simulated));
        _physics.OnUpdate = _ =>
        {
            _physics.SetPosition(HandleOf(root), new Vector3(5, 0, 0));
            _physics.SetPosition(HandleOf(child), new Vector3(6, 0, 0));
        };

        Tick();

        Assert.That(_world.Get<LocalTransform>(child).Position, Is.EqualTo(new Vector3(1, 0, 0)));
    }

    [Test]
    public void AnUnchangedSimulatedPoseDoesNotDirtyTheTransform()
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, new Vector3(1, 2, 3));
        _world.Add(entity, Box(PhysicsState.Simulated));
        Tick();
        var version = _world.Get<WorldTransform>(entity).Version;

        Tick();

        Assert.That(_world.Get<WorldTransform>(entity).Version, Is.EqualTo(version));
        Assert.That(_world.Get<LocalTransform>(entity).IsDirty, Is.False);
    }

    [Test]
    public void ADirtyVelocityIsPushedBeforeTheStep()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new Velocity());
        _world.Add(entity, Box(PhysicsState.Simulated));
        var stepped = Vector3.Zero;
        _physics.OnUpdate = _ => stepped = _physics.GetLinearVelocity(HandleOf(entity));

        _world.Get<Velocity>(entity).Linear = new Vector3(4, 5, 6);
        Tick();

        Assert.That(stepped, Is.EqualTo(new Vector3(4, 5, 6)));
        Assert.That(_world.Get<Velocity>(entity).IsDirty, Is.False);
    }

    [Test]
    public void ASimulatedBodyWritesItsSteppedVelocityBackWithoutMarkingItDirty()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new Velocity());
        _world.Add(entity, Box(PhysicsState.Simulated));
        _physics.OnUpdate = _ =>
        {
            _physics.SetLinearVelocity(HandleOf(entity), new Vector3(7, 0, 0));
            _physics.SetAngularVelocity(HandleOf(entity), new Vector3(0, 0, 1));
        };

        Tick();

        var velocity = _world.Get<Velocity>(entity);
        Assert.That(velocity.Linear, Is.EqualTo(new Vector3(7, 0, 0)));
        Assert.That(velocity.Angular, Is.EqualTo(new Vector3(0, 0, 1)));
        Assert.That(velocity.IsDirty, Is.False);
    }

    [Test]
    public void ACleanVelocityIsNotPushedOverTheBodysOwnVelocity()
    {
        var entity = _world.Spawn();
        _world.Add(entity, new Velocity());
        _world.Add(entity, Box(PhysicsState.Controlled));

        _physics.SetLinearVelocity(HandleOf(entity), new Vector3(9, 0, 0));
        Tick();

        Assert.That(_physics.GetLinearVelocity(HandleOf(entity)), Is.EqualTo(new Vector3(9, 0, 0)));
    }

    [Test]
    public void TheStepUsesTheTickInterval()
    {
        var entity = _world.Spawn();
        _world.Add(entity, Box(PhysicsState.Static));
        var steps = new List<float>();
        _physics.OnUpdate = steps.Add;
        _world.TickInterval = 0.1f;

        _world.Update(0.25f);

        Assert.That(steps, Is.EqualTo(new[] { 0.1f, 0.1f }));
    }

    [Test]
    public void PhysicsTicksDoNotAllocate()
    {
        var parent = RotatedParentAt(new Vector3(10, 0, 0));
        for (var i = 0; i < 50; i++)
        {
            var entity = _world.Spawn();
            _world.SetPosition(entity, new Vector3(i, 0, 0));
            if (i % 2 == 0) _world.SetParent(entity, parent);
            _world.Add(entity, new Velocity { Linear = Vector3.UnitY });
            _world.Add(entity, Box(i % 3 == 0 ? PhysicsState.Controlled : PhysicsState.Simulated));
        }

        for (var i = 0; i < 10; i++) Tick();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++) Tick();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }
}
