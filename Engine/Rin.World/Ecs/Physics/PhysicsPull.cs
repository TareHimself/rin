using System.Numerics;
using Rin.World.Ecs.Scheduling;
using Rin.World.Ecs.Transforms;
using Rin.World.Physics;

namespace Rin.World.Ecs.Physics;

/// <summary>
///     Writes the pose and velocity of <see cref="PhysicsState.Simulated" /> bodies back into their entity after the
///     step. A child's world pose is converted into its parent's space; its scale is left alone.
/// </summary>
/// <remarks>
///     Roots are written before children so a child converts against its parent's new pose. A simulated body below
///     another simulated child may convert against a pose one tick old.
/// </remarks>
internal sealed class PhysicsPull : TickSystem
{
    private readonly EntityWorld _world;
    private readonly IPhysicsSystem _physics;
    private readonly Query<PhysicsBody, LocalTransform> _poses;
    private readonly Query<PhysicsBody, Velocity> _velocities;

    public PhysicsPull(EntityWorld world, IPhysicsSystem physics)
    {
        _world = world;
        _physics = physics;
        _poses = world.Query<PhysicsBody, LocalTransform>();
        _velocities = world.Query<PhysicsBody, Velocity>();
    }

    public override void Tick(in TickContext context)
    {
        PullPoses(true);
        PullPoses(false);
        PullVelocities();
    }

    private void PullPoses(bool roots)
    {
        foreach (var (bodies, locals, entities) in _poses)
            for (var i = 0; i < bodies.Length; i++)
            {
                ref readonly var body = ref bodies[i];
                if (body.State != PhysicsState.Simulated) continue;

                var parent = _world.GetParent(entities[i]);
                if (parent.IsValid == roots) continue;

                var position = _physics.GetPosition(body.Handle);
                var rotation = _physics.GetOrientation(body.Handle);
                if (parent.IsValid) ToParentSpace(parent, ref position, ref rotation);

                // Writing only real changes keeps resting bodies from dirtying propagation every tick.
                ref var local = ref locals[i];
                if (local.Position != position) local.Position = position;
                if (local.Rotation != rotation) local.Rotation = rotation;
            }
    }

    private void ToParentSpace(Entity parent, ref Vector3 position, ref Quaternion rotation)
    {
        var parentWorld = _world.ComputeWorldMatrix(parent);
        if (!Matrix4x4.Invert(parentWorld, out var worldToParent)) return;
        Matrix4x4.Decompose(parentWorld, out _, out var parentRotation, out _);

        position = Vector3.Transform(position, worldToParent);
        rotation = Quaternion.Normalize(Quaternion.Conjugate(parentRotation) * rotation);
    }

    private void PullVelocities()
    {
        foreach (var (bodies, velocities) in _velocities)
            for (var i = 0; i < bodies.Length; i++)
            {
                ref readonly var body = ref bodies[i];
                if (body.State != PhysicsState.Simulated) continue;
                velocities[i].PullFromBody(_physics.GetLinearVelocity(body.Handle), _physics.GetAngularVelocity(body.Handle));
            }
    }
}
