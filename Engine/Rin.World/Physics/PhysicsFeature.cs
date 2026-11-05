using Rin.Core.Shared.Math;
using Rin.World.Scheduling;
using Rin.World.Transforms;

namespace Rin.World.Physics;

/// <summary>
///     Connects an <see cref="World" /> to an <see cref="IPhysicsSystem" />. Hooks create a body and a
///     <see cref="PhysicsBody" /> component when a <see cref="RigidBody" /> is added and destroy both when it is removed or
///     despawned. Four <see cref="TickPhase.Physics" /> systems run in order: <see cref="PhysicsPush" />, the step,
///     <see cref="PhysicsPull" /> and <see cref="ContactDispatch" />.
/// </summary>
/// <remarks>
///     A snapshot restore destroys every entity body and creates a new one from <see cref="RigidBody" />, the restored
///     world pose and <see cref="Velocity" />, and clears <see cref="Contacts" />. The physics system's own state (warm
///     starting, sleeping, contact history) is not restored, so a restored run matches another run that restored the same
///     way, but not a run that never restored.
/// </remarks>
public sealed class PhysicsFeature : IWorldRestoreListener
{
    private readonly ContactDispatch _contacts;
    private readonly Query<PhysicsBody, RigidBody> _bodies;

    public PhysicsFeature(World world, IPhysicsSystem physicsSystem)
    {
        World = world;
        PhysicsSystem = physicsSystem;
        _contacts = new ContactDispatch(physicsSystem, Bodies);

        world.OnAdd<RigidBody>(CreateBody);
        world.OnRemove<RigidBody>(DestroyBody);
        world.AddSystem(TickPhase.Physics, new PhysicsPush(world, physicsSystem));
        world.AddSystem(TickPhase.Physics, new PhysicsStep(physicsSystem));
        world.AddSystem(TickPhase.Physics, new PhysicsPull(world, physicsSystem));
        world.AddSystem(TickPhase.Physics, _contacts);
        world.AddRestoreListener(this);
        _bodies = world.Query<PhysicsBody, RigidBody>();
    }

    public World World { get; }

    public IPhysicsSystem PhysicsSystem { get; }

    /// <summary>
    ///     The contacts of the last physics step. Written at the end of a tick's physics phase and kept until the next
    ///     one, so the next tick's gameplay sees them. An entity in a contact may have been despawned since.
    /// </summary>
    public ReadOnlySpan<EntityContact> Contacts => _contacts.Contacts;

    /// <summary>
    ///     The entity that owns <paramref name="body" />, or <c>default</c>.
    /// </summary>
    public Entity FindEntity(PhysicsBodyHandle body) => Bodies.Find(body);

    internal BodyEntityMap Bodies { get; } = new();

    void IWorldRestoreListener.BeforeRestore()
    {
        foreach (var (bodies, _) in _bodies)
            for (var i = 0; i < bodies.Length; i++)
            {
                var handle = bodies[i].Handle;
                if (!handle.IsValid) continue;
                Bodies.Remove(handle);
                PhysicsSystem.DestroyBody(handle);
            }

        _contacts.Clear();
    }

    void IWorldRestoreListener.AfterRestore()
    {
        foreach (var (bodies, rigidBodies, entities) in _bodies)
            for (var i = 0; i < bodies.Length; i++)
            {
                var entity = entities[i];
                var handle = CreateBodyHandle(entity, rigidBodies[i]);
                Bodies.Add(handle, entity);
                bodies[i] = new PhysicsBody { Handle = handle, State = rigidBodies[i].State };
            }
    }

    private void CreateBody(Entity entity, ref RigidBody rigidBody)
    {
        var handle = CreateBodyHandle(entity, rigidBody);
        Bodies.Add(handle, entity);
        World.Add(entity, new PhysicsBody { Handle = handle, State = rigidBody.State });
    }

    private PhysicsBodyHandle CreateBodyHandle(Entity entity, in RigidBody desc)
    {
        var pose = Transform.From(World.ComputeWorldMatrix(entity));
        var handle = desc.Shape.Kind switch
        {
            ColliderShapeKind.Box => PhysicsSystem.CreateBox(desc.Shape.Size, pose, desc.State),
            ColliderShapeKind.Sphere => PhysicsSystem.CreateSphere(desc.Shape.Radius, pose, desc.State),
            ColliderShapeKind.Capsule => PhysicsSystem.CreateCapsule(desc.Shape.Radius, desc.Shape.HalfHeight, pose, desc.State),
            _ => throw new ArgumentOutOfRangeException(nameof(desc), desc.Shape.Kind, "Unknown collider shape.")
        };

        PhysicsSystem.SetMass(handle, desc.Mass);
        PhysicsSystem.SetTrigger(handle, desc.IsTrigger);
        PhysicsSystem.SetCollisionChannel(handle, desc.CollisionChannel);
        if (World.Has<Velocity>(entity))
        {
            ref var velocity = ref World.Get<Velocity>(entity);
            PhysicsSystem.SetLinearVelocity(handle, velocity.Linear);
            PhysicsSystem.SetAngularVelocity(handle, velocity.Angular);
            velocity.IsDirty = false;
        }

        return handle;
    }

    private void DestroyBody(Entity entity, ref RigidBody rigidBody)
    {
        if (!World.Has<PhysicsBody>(entity)) return;
        var handle = World.Get<PhysicsBody>(entity).Handle;
        Bodies.Remove(handle);
        PhysicsSystem.DestroyBody(handle);
        World.Remove<PhysicsBody>(entity);
    }
}
