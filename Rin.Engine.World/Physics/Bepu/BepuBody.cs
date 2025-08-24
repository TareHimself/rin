using System.Diagnostics;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Framework.Math;

namespace Rin.Engine.World.Physics.Bepu;

public abstract class BepuBody : IPhysicsBody
{
    protected readonly BepuPhysicsSystem System;
    private float _mass = 1.0f;
    private PhysicsState _state;
    public BodyHandle? BodyHandle;
    public StaticHandle? StaticHandle;

    public BepuBody(PhysicsState state, Transform transform, BepuPhysicsSystem system)
    {
        _state = state;
        _position = transform.Position;
        _scale = transform.Scale;
        _orientation = transform.Orientation;
        System = system;
    }

    private Vector3 _scale { get; set; }
    private Quaternion _orientation { get; set; }
    private Vector3 _position { get; set; }

    public void Dispose()
    {
        switch (GetState())
        {
            case PhysicsState.Static:
                System.Simulation.Statics.Remove(StaticHandle ?? throw new NullReferenceException());
                break;
            case PhysicsState.Controlled or PhysicsState.Simulated:
                System.Simulation.Bodies.Remove(BodyHandle ?? throw new NullReferenceException());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        System.Simulation.Shapes.Remove(GetShapeIndex());
    }

    public int CollisionChannel { get; set; }

    public void ProcessHit(RayCastResult result)
    {
        throw new NotImplementedException();
    }

    public Vector3 GetLinearVelocity()
    {
        return GetState() switch
        {
            PhysicsState.Static => Vector3.Zero,
            PhysicsState.Controlled => GetBodyReference().Velocity.Linear,
            PhysicsState.Simulated => GetBodyReference().Velocity.Linear,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public Vector3 GetAngularVelocity()
    {
        return GetState() switch
        {
            PhysicsState.Static => Vector3.Zero,
            PhysicsState.Controlled => GetBodyReference().Velocity.Angular,
            PhysicsState.Simulated => GetBodyReference().Velocity.Angular,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public Vector3 GetPosition()
    {
        if (!HasAnyHandle()) return _position;

        return _position = GetState() switch
        {
            PhysicsState.Static => GetStaticReference().Pose.Position,
            PhysicsState.Controlled or PhysicsState.Simulated => GetBodyReference().Pose.Position,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public Quaternion GetOrientation()
    {
        if (!HasAnyHandle()) return _orientation;

        return _orientation = GetState() switch
        {
            PhysicsState.Static => GetStaticReference().Pose.Orientation,
            PhysicsState.Controlled or PhysicsState.Simulated => GetBodyReference().Pose.Orientation,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public Vector3 GetScale()
    {
        return _scale;
    }

    public PhysicsState GetState()
    {
        return _state;
    }

    public float GetMass()
    {
        return _mass;
    }

    public void SetLinearVelocity(in Vector3 velocity)
    {
        switch (GetState())
        {
            case PhysicsState.Static:
                break;
            case PhysicsState.Controlled or PhysicsState.Simulated:
                GetBodyReference().Velocity.Linear = velocity;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetAngularVelocity(in Vector3 angularVelocity)
    {
        switch (GetState())
        {
            case PhysicsState.Static:
                break;
            case PhysicsState.Controlled or PhysicsState.Simulated:
                GetBodyReference().Velocity.Angular = angularVelocity;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetPosition(in Vector3 position)
    {
        _position = position;
        switch (GetState())
        {
            case PhysicsState.Static:
                GetStaticReference().Pose.Position = position;
                break;
            case PhysicsState.Controlled or PhysicsState.Simulated:
                GetBodyReference().Pose.Position = position;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetOrientation(in Quaternion orientation)
    {
        _orientation = orientation;
        switch (GetState())
        {
            case PhysicsState.Static:
                GetStaticReference().Pose.Orientation = orientation;
                break;
            case PhysicsState.Controlled or PhysicsState.Simulated:
                GetBodyReference().Pose.Orientation = orientation;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetScale(in Vector3 scale)
    {
        _scale = scale;
        ScaleUpdated();
    }

    public void SetMass(float mass)
    {
        _mass = mass;
        UpdateInertia();
    }

    public void SetState(PhysicsState state)
    {
        var oldState = _state;
        _state = state;
        if (oldState == _state) return;

        if (oldState == PhysicsState.Static)
        {
        }
    }

    protected abstract TypedIndex GetShapeIndex();
    protected abstract void UpdateShape();
    protected abstract void ScaleUpdated();

    protected bool HasAnyHandle()
    {
        return BodyHandle.HasValue || StaticHandle.HasValue;
    }

    protected BodyReference GetBodyReference()
    {
        return GetState() switch
        {
            PhysicsState.Static => throw new NullReferenceException(),
            PhysicsState.Controlled or PhysicsState.Simulated => System.Simulation.Bodies[
                BodyHandle ?? throw new NullReferenceException()],
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected StaticReference GetStaticReference()
    {
        return GetState() switch
        {
            PhysicsState.Static => System.Simulation.Statics[StaticHandle ?? throw new NullReferenceException()],
            PhysicsState.Controlled or PhysicsState.Simulated => throw new NullReferenceException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected BodyDescription CreateBodyDescription()
    {
        var state = GetState();
        var pose = new RigidPose(GetPosition(), GetOrientation());
        var collidable = new CollidableDescription(GetShapeIndex(), ContinuousDetection.Continuous());
        var bodyActivityDescription = new BodyActivityDescription(0.01f);
        return state switch
        {
            PhysicsState.Controlled => BodyDescription.CreateKinematic(pose, collidable, bodyActivityDescription),
            PhysicsState.Simulated => BodyDescription.CreateDynamic(pose, ComputeInertia(), collidable,
                bodyActivityDescription),
            PhysicsState.Static => throw new UnreachableException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected StaticDescription CreateStaticDescription()
    {
        return new StaticDescription
        {
            Pose = new RigidPose(GetPosition(), GetOrientation()),
            Shape = GetShapeIndex(),
            Continuity = ContinuousDetection.Continuous()
        };
    }

    protected abstract BodyInertia ComputeInertia();

    public void Init()
    {
        switch (GetState())
        {
            case PhysicsState.Static:
                StaticHandle = System.Simulation.Statics.Add(CreateStaticDescription());
                break;
            case PhysicsState.Controlled or PhysicsState.Simulated:
                BodyHandle = System.Simulation.Bodies.Add(CreateBodyDescription());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected void UpdateInertia()
    {
        switch (GetState())
        {
            case PhysicsState.Static:
                break;
            case PhysicsState.Controlled or PhysicsState.Simulated:
                GetBodyReference().LocalInertia = ComputeInertia();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}