using Rin.Core.Shared.Math;
using Rin.World.Scheduling;
using Rin.World.Transforms;

namespace Rin.World.Physics;

/// <summary>
///     Connects an <see cref="World" /> to an <see cref="IPhysicsSystem" />. Hooks create a body and a
///     <see cref="PhysicsBody" /> component once an entity has a <see cref="RigidBody" /> and a collider component, in
///     either order, and destroy both when either is removed or the entity despawns. Four <see cref="TickPhase.Physics" />
///     systems run in order: <see cref="PhysicsPush" />, the step, <see cref="PhysicsPull" /> and
///     <see cref="ContactDispatch" />.
/// </summary>
/// <remarks>
///     A <see cref="RigidBody" /> without a collider has no body until one is added. A snapshot restore destroys every
///     entity body and creates a new one from <see cref="RigidBody" />, the collider, the restored world pose and
///     <see cref="Velocity" />, and clears <see cref="Contacts" />. The physics system's own state (warm
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

        world.OnAdd<RigidBody>(OnBodyPartAdded);
        world.OnAdd<BoxCollider>(OnBodyPartAdded);
        world.OnAdd<SphereCollider>(OnBodyPartAdded);
        world.OnAdd<CapsuleCollider>(OnBodyPartAdded);
        world.OnAdd<CompoundCollider>(OnBodyPartAdded);
        world.OnRemove<RigidBody>(OnBodyPartRemoved);
        world.OnRemove<BoxCollider>(OnBodyPartRemoved);
        world.OnRemove<SphereCollider>(OnBodyPartRemoved);
        world.OnRemove<CapsuleCollider>(OnBodyPartRemoved);
        world.OnRemove<CompoundCollider>(OnBodyPartRemoved);
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
    ///     The parts of every <see cref="CompoundCollider" />.
    /// </summary>
    public PhysicsShapes Shapes { get; } = new();

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
        foreach (var chunk in _bodies)
            for (var i = 0; i < chunk.Length; i++)
            {
                var entity = chunk.Entities[i];
                ref readonly var rigidBody = ref chunk.Components2[i];
                if (Colliders.TryGetShape(World, entity, out var shape))
                    chunk.Write<PhysicsBody>(i) = CreatePhysicsBody(entity, rigidBody, shape);
            }
    }

    private void OnBodyPartAdded<T>(Entity entity, in T _) where T : unmanaged, IComponentData
    {
        if (World.Has<PhysicsBody>(entity) || !World.Has<RigidBody>(entity)) return;
        if (!Colliders.TryGetShape(World, entity, out var shape)) return;

        World.Add(entity, CreatePhysicsBody(entity, World.Get<RigidBody>(entity), shape));
    }

    private void OnBodyPartRemoved<T>(Entity entity, in T _) where T : unmanaged, IComponentData => DestroyBody(entity);

    private PhysicsBody CreatePhysicsBody(Entity entity, in RigidBody rigidBody, in ColliderShape shape)
    {
        var handle = CreateBodyHandle(entity, rigidBody, shape);
        Bodies.Add(handle, entity);
        var body = new PhysicsBody { Handle = handle, State = rigidBody.State };
        if (!World.Has<Velocity>(entity)) return body;

        ref readonly var velocity = ref World.Get<Velocity>(entity);
        PhysicsSystem.SetLinearVelocity(handle, velocity.Linear);
        PhysicsSystem.SetAngularVelocity(handle, velocity.Angular);
        body.SyncedVelocity = velocity;
        return body;
    }

    private PhysicsBodyHandle CreateBodyHandle(Entity entity, in RigidBody desc, in ColliderShape shape)
    {
        var pose = Transform.From(World.ComputeWorldMatrix(entity));
        var handle = shape.Kind switch
        {
            ColliderShapeKind.Box => PhysicsSystem.CreateBox(shape.Size, pose, desc.State),
            ColliderShapeKind.Sphere => PhysicsSystem.CreateSphere(shape.Radius, pose, desc.State),
            ColliderShapeKind.Capsule => PhysicsSystem.CreateCapsule(shape.Radius, shape.HalfHeight, pose, desc.State),
            ColliderShapeKind.Compound => PhysicsSystem.CreateCompound(Shapes.PartsOf(shape.Compound), pose, desc.State),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape.Kind, "Unknown collider shape.")
        };

        PhysicsSystem.SetMass(handle, desc.Mass);
        PhysicsSystem.SetTrigger(handle, desc.IsTrigger);
        PhysicsSystem.SetCollisionChannel(handle, desc.CollisionChannel);
        return handle;
    }

    private void DestroyBody(Entity entity)
    {
        if (!World.Has<PhysicsBody>(entity)) return;
        var handle = World.Get<PhysicsBody>(entity).Handle;
        Bodies.Remove(handle);
        PhysicsSystem.DestroyBody(handle);
        World.Remove<PhysicsBody>(entity);
    }
}
