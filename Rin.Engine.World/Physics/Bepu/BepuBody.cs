using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Math;
using Rin.Engine.World.Components;
using Rin.Engine.World.Math;

namespace Rin.Engine.World.Physics.Bepu;

public abstract class BepuBody(BepuPhysics physics, IPhysicsComponent owner) : IPhysicsBody
{
    [PublicAPI] protected readonly BepuPhysics Physics = physics;
    private Vector3 _angularVelocity = Vector3.Zero;

    private Vector3 _linearVelocity = Vector3.Zero;
    private bool _simulating;
    private bool _static;
    [PublicAPI] protected StaticHandle? StaticBodyHandle { get; set; }
    [PublicAPI] protected BodyHandle? DynamicBodyHandle { get; set; }

    public bool IsValid => StaticBodyHandle.HasValue || DynamicBodyHandle.HasValue;

    public IPhysicsComponent Owner { get; set; } = owner;

    public Vector3 LinearVelocity
    {
        get
        {
            if (!IsStatic && DynamicBodyHandle is { } handle)
            {
                var vel = Physics.Simulation.Bodies[handle].Velocity.Linear;
                _linearVelocity = new Vector3(vel.X, vel.Y, vel.Z);
            }

            return _linearVelocity;
        }
        set
        {
            _linearVelocity = value;
            if (!IsStatic && DynamicBodyHandle is { } handle) Physics.Simulation.Bodies[handle].Velocity.Linear = value;
        }
    }

    public Vector3 AngularVelocity
    {
        get
        {
            if (!IsStatic && DynamicBodyHandle is { } handle)
            {
                var vel = Physics.Simulation.Bodies[handle].Velocity.Angular;
                _angularVelocity = new Vector3(vel.X, vel.Y, vel.Z);
            }

            return _angularVelocity;
        }
        set
        {
            _angularVelocity = value;
            if (!IsStatic && DynamicBodyHandle is { } handle)
                Physics.Simulation.Bodies[handle].Velocity.Angular = value;
        }
    }

    public bool IsStatic
    {
        get => _static;
        set
        {
            var changed = _static != value;
            if (changed)
            {
                TryUpdateVelocityFromBody();
                DestroyExistingBody();
            }

            _static = value;
            if (changed)
            {
                if (_static)
                {
                }

                CreateBody();
            }
        }
    }

    public bool IsSimulating => _simulating && !IsStatic;

    public float Mass { get; set; } = 1.0f;
    public int CollisionChannel { get; set; } = 0;

    public void SetSimulatePhysics(bool simulate)
    {
        var state = IsSimulating;
        if (state == simulate) return;
        if (IsStatic && simulate) return;
        TryUpdateVelocityFromBody();
        UpdateBodyDescription(simulate);
        _simulating = simulate;
    }

    public Transform GetTransform()
    {
        var pose = GetRigidPose();
        return new Transform
        {
            Location = pose.Position,
            Rotation = pose.Orientation,
            Scale = Owner.GetTransform(Space.World).Scale
        };
    }

    public void ProcessHit(RayCastResult result)
    {
        Owner.ProcessHit(this, result);
    }

    public virtual void Dispose()
    {
        DestroyExistingBody();
    }

    public void Init()
    {
        CreateBody();
    }

    private RigidPose GetRigidPose()
    {
        if (IsStatic)
        {
            if (StaticBodyHandle is { } handle) return Physics.Simulation.Statics[handle].Pose;
        }
        else
        {
            if (DynamicBodyHandle is { } handle) return Physics.Simulation.Bodies[handle].Pose;
        }

        return RigidPose.Identity;
    }

    protected abstract TypedIndex GetTypedIndex();

    protected abstract BodyInertia ComputeInertia();

    [PublicAPI]
    protected RigidPose RigidPoseFromComponentTransform()
    {
        var transform = Owner.GetTransform(Space.World);
        return new RigidPose(transform.Location, transform.Rotation);
    }

    protected virtual StaticDescription MakeStaticDescription(TypedIndex typedIndex)
    {
        return new StaticDescription(RigidPoseFromComponentTransform(), typedIndex);
    }

    protected virtual BodyDescription MakeDynamicDescription(TypedIndex typedIndex, bool? simulating = false)
    {
        var collidableDescription = new CollidableDescription(typedIndex);
        var bodyActivityDescription = new BodyActivityDescription(0.01f);
        var pose = RigidPoseFromComponentTransform();
        if (simulating.GetValueOrDefault(IsSimulating))
            return BodyDescription.CreateDynamic(pose,
                new BodyVelocity(LinearVelocity, AngularVelocity),
                ComputeInertia(), collidableDescription, bodyActivityDescription);

        return BodyDescription.CreateKinematic(pose, collidableDescription, bodyActivityDescription);
    }

    [PublicAPI]
    protected void TryUpdateVelocityFromBody()
    {
        // if (IsStatic || DynamicBodyHandle is not { } handle) return;
        // {
        //     var vel = Physics.Simulation.Bodies[handle].Velocity.Linear;
        //     _linearVelocity = new Vector3(vel.X, vel.Y, vel.Z);
        // }
        // {
        //     var vel = Physics.Simulation.Bodies[handle].Velocity.Angular;
        //     _angularVelocity = new Vector3(vel.X, vel.Y, vel.Z);
        // }
    }

    protected virtual void UpdateBodyDescription(bool? simulating = false)
    {
        if (IsStatic)
        {
            if (StaticBodyHandle is { } handle)
                Physics.Simulation.Statics.ApplyDescription(handle, MakeStaticDescription(GetTypedIndex()));
        }
        else
        {
            if (DynamicBodyHandle is { } handle)
                Physics.Simulation.Bodies.ApplyDescription(handle, MakeDynamicDescription(GetTypedIndex(), simulating));
        }
    }

    [PublicAPI]
    protected void DestroyExistingBody()
    {
        if (IsStatic)
        {
            if (StaticBodyHandle is { } handle) Physics.RemoveStatic(this, handle);

            StaticBodyHandle = null;
        }
        else
        {
            if (DynamicBodyHandle is { } handle) Physics.RemoveDynamic(this, handle);

            DynamicBodyHandle = null;
        }
    }

    protected virtual void CreateBody()
    {
        if (IsStatic)
            StaticBodyHandle = Physics.AddStatic(this, MakeStaticDescription(GetTypedIndex()));
        else
            DynamicBodyHandle = Physics.AddDynamic(this, MakeDynamicDescription(GetTypedIndex()));
    }
}