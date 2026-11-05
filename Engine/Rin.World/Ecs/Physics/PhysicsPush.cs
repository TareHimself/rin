using System.Numerics;
using Rin.World.Ecs.Scheduling;
using Rin.World.Ecs.Transforms;
using Rin.World.Physics;

namespace Rin.World.Ecs.Physics;

/// <summary>
///     Moves <see cref="PhysicsState.Controlled" /> bodies to their entity's world pose and pushes dirty
///     <see cref="Velocity" /> components, before the step.
/// </summary>
/// <remarks>
///     <see cref="WorldTransform" /> is only propagated per frame, so a child's pose is computed from the
///     <see cref="LocalTransform" /> chain, one lookup per ancestor. A root uses its local transform directly.
/// </remarks>
internal sealed class PhysicsPush : TickSystem
{
    private readonly EntityWorld _world;
    private readonly IPhysicsSystem _physics;
    private readonly Query<PhysicsBody, LocalTransform> _poses;
    private readonly Query<PhysicsBody, Velocity> _velocities;

    public PhysicsPush(EntityWorld world, IPhysicsSystem physics)
    {
        _world = world;
        _physics = physics;
        _poses = world.Query<PhysicsBody, LocalTransform>();
        _velocities = world.Query<PhysicsBody, Velocity>();
    }

    public override void Tick(in TickContext context)
    {
        PushControlledPoses();
        PushDirtyVelocities();
    }

    private void PushControlledPoses()
    {
        foreach (var (bodies, locals, entities) in _poses)
            for (var i = 0; i < bodies.Length; i++)
            {
                ref readonly var body = ref bodies[i];
                if (body.State != PhysicsState.Controlled) continue;

                var entity = entities[i];
                Vector3 position;
                Quaternion rotation;
                if (_world.GetParent(entity).IsValid)
                {
                    Matrix4x4.Decompose(_world.ComputeWorldMatrix(entity), out _, out rotation, out position);
                }
                else
                {
                    position = locals[i].Position;
                    rotation = locals[i].Rotation;
                }

                _physics.SetPosition(body.Handle, position);
                _physics.SetOrientation(body.Handle, rotation);
            }
    }

    private void PushDirtyVelocities()
    {
        foreach (var (bodies, velocities) in _velocities)
            for (var i = 0; i < bodies.Length; i++)
            {
                ref var velocity = ref velocities[i];
                if (!velocity.IsDirty) continue;

                _physics.SetLinearVelocity(bodies[i].Handle, velocity.Linear);
                _physics.SetAngularVelocity(bodies[i].Handle, velocity.Angular);
                velocity.IsDirty = false;
            }
    }
}
