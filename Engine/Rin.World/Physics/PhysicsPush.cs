using System.Numerics;
using Rin.World.Scheduling;
using Rin.World.Transforms;

namespace Rin.World.Physics;

/// <summary>
///     Moves <see cref="PhysicsState.Controlled" /> bodies to their entity's world pose and pushes
///     <see cref="Velocity" /> components that changed since the body last saw them, before the step.
/// </summary>
/// <remarks>
///     <see cref="WorldTransform" /> is only propagated per frame, so a child's pose is computed from the
///     <see cref="LocalTransform" /> chain, one lookup per ancestor. A root uses its local transform directly.
/// </remarks>
internal sealed class PhysicsPush : TickSystem
{
    private readonly World _world;
    private readonly IPhysicsSystem _physics;
    private readonly Query<PhysicsBody, LocalTransform> _poses;
    private readonly Query<PhysicsBody, Velocity> _velocities;

    public PhysicsPush(World world, IPhysicsSystem physics)
    {
        _world = world;
        _physics = physics;
        _poses = world.Query<PhysicsBody, LocalTransform>();
        _velocities = world.Query<PhysicsBody, Velocity>();
    }

    public override void Tick(in TickContext context)
    {
        PushControlledPoses();
        PushChangedVelocities();
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

    private void PushChangedVelocities()
    {
        foreach (var chunk in _velocities)
            for (var i = 0; i < chunk.Length; i++)
            {
                ref readonly var velocity = ref chunk.Components2[i];
                if (velocity.Matches(chunk.Components1[i].SyncedVelocity)) continue;

                ref var body = ref chunk.Write<PhysicsBody>(i);
                _physics.SetLinearVelocity(body.Handle, velocity.Linear);
                _physics.SetAngularVelocity(body.Handle, velocity.Angular);
                body.SyncedVelocity = velocity;
            }
    }
}
