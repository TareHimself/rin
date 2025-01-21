using BepuPhysics;
using BepuPhysics.Collidables;
using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics.Bepu;

public abstract class BepuBody(BepuPhysics physics, IPhysicsComponent owner) : IPhysicsBody
{
    [PublicAPI] protected readonly BepuPhysics Physics = physics;

    public IPhysicsComponent Owner { get; set; } = owner;

    private Vec3<float> _linearVelocity = Vec3<float>.Zero;
    private Vec3<float> _angularVelocity = Vec3<float>.Zero;
    private bool _static = false;
    private bool _simulating = false;
    [PublicAPI] protected StaticHandle? StaticBodyHandle { get; set; } = null;
    [PublicAPI] protected BodyHandle? DynamicBodyHandle { get; set; } = null;
    
    public bool IsValid => StaticBodyHandle.HasValue || DynamicBodyHandle.HasValue;

    public Vec3<float> LinearVelocity
    {
        get
        {
            if (!IsStatic && DynamicBodyHandle is { } handle)
            {
                var vel = Physics.Simulation.Bodies[handle].Velocity.Linear;
                _linearVelocity = new Vec3<float>(vel.X, vel.Y, vel.Z);
            }

            return _linearVelocity.Clone();
        }
        set
        {
            _linearVelocity = value;
            if (!IsStatic && DynamicBodyHandle is { } handle)
            {
                Physics.Simulation.Bodies[handle].Velocity.Linear = value.ToVector3();
            }
        }
    }

    public Vec3<float> AngularVelocity
    {
        get
        {
            if (!IsStatic && DynamicBodyHandle is { } handle)
            {
                var vel = Physics.Simulation.Bodies[handle].Velocity.Angular;
                _angularVelocity = new Vec3<float>(vel.X, vel.Y, vel.Z);
            }

            return _angularVelocity.Clone();
        }
        set
        {
            _angularVelocity = value;
            if (!IsStatic && DynamicBodyHandle is { } handle)
            {
                Physics.Simulation.Bodies[handle].Velocity.Angular = value.ToVector3();
            }
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

    public void Init()
    {
        CreateBody();
    }

    public void SetSimulatePhysics(bool simulate)
    {
        var state = IsSimulating;
        if (state == simulate) return;
        if (IsStatic && simulate) return;
        TryUpdateVelocityFromBody();
        UpdateBodyDescription(simulate);
        _simulating = simulate;
    }

    private RigidPose GetRigidPose()
    {
        if (IsStatic)
        {
            if (StaticBodyHandle is { } handle)
            {
                return Physics.Simulation.Statics[handle].Pose;
            }
        }
        else
        {
            if (DynamicBodyHandle is { } handle)
            {
                return Physics.Simulation.Bodies[handle].Pose;
            }
        }

        return RigidPose.Identity;
    }

    public Transform GetTransform()
    {
        var pose = GetRigidPose();
        return new Transform()
        {
            Location = new Vec3<float>(pose.Position.X, pose.Position.Y, pose.Position.Z),
            Rotation =
                (Rotator)new Quat(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W),
            Scale = Owner.GetSceneTransform().Scale
        };
    }

    public void ProcessHit(RayCastResult result)
    {
        Owner.ProcessHit(this,result);
    }

    public virtual void Dispose()
    {
        DestroyExistingBody();
    }

    protected abstract TypedIndex GetTypedIndex();

    protected abstract BodyInertia ComputeInertia();

    [PublicAPI]
    protected RigidPose RigidPoseFromComponentTransform()
    {
        var transform = Owner.GetSceneTransform();
        return new RigidPose(transform.Location.ToVector3(), ((Quat)transform.Rotation).ToQuaternion());
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
        {
            return BodyDescription.CreateDynamic(pose,
                new BodyVelocity(LinearVelocity.ToVector3(), AngularVelocity.ToVector3()),
                ComputeInertia(), collidableDescription, bodyActivityDescription);
        }
        else
        {
            return BodyDescription.CreateKinematic(pose, collidableDescription, bodyActivityDescription);
        }
    }

    [PublicAPI]
    protected void TryUpdateVelocityFromBody()
    {
        // if (IsStatic || DynamicBodyHandle is not { } handle) return;
        // {
        //     var vel = Physics.Simulation.Bodies[handle].Velocity.Linear;
        //     _linearVelocity = new Vec3<float>(vel.X, vel.Y, vel.Z);
        // }
        // {
        //     var vel = Physics.Simulation.Bodies[handle].Velocity.Angular;
        //     _angularVelocity = new Vec3<float>(vel.X, vel.Y, vel.Z);
        // }
    }

    protected virtual void UpdateBodyDescription(bool? simulating = false)
    {
        if (IsStatic)
        {
            if (StaticBodyHandle is { } handle)
            {
                Physics.Simulation.Statics.ApplyDescription(handle, MakeStaticDescription(GetTypedIndex()));
            }
        }
        else
        {
            if (DynamicBodyHandle is { } handle)
            {
                Physics.Simulation.Bodies.ApplyDescription(handle, MakeDynamicDescription(GetTypedIndex(),simulating));
            }
        }
    }

    [PublicAPI]
    protected void DestroyExistingBody()
    {
        if (IsStatic)
        {
            if (StaticBodyHandle is { } handle)
            {
                Physics.RemoveStatic(this,handle);
            }

            StaticBodyHandle = null;
        }
        else
        {
            if (DynamicBodyHandle is { } handle)
            {
                Physics.RemoveDynamic(this,handle);
            }

            DynamicBodyHandle = null;
        }
    }

    protected virtual void CreateBody()
    {
        if (IsStatic)
        {
            StaticBodyHandle = Physics.AddStatic(this,MakeStaticDescription(GetTypedIndex()));
        }
        else
        {
            DynamicBodyHandle = Physics.AddDynamic(this,MakeDynamicDescription(GetTypedIndex()));
        }
    }
}