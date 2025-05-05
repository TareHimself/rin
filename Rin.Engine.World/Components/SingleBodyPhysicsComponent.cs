using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Math;
using Rin.Engine.World.Math;
using Rin.Engine.World.Physics;

namespace Rin.Engine.World.Components;

public abstract class SingleBodyPhysicsComponent : SceneComponent, IPhysicsComponent
{
    private Vector3 _angularVelocity;
    private Vector3 _linearVelocity;
    private float _mass = 1.0f;
    private PhysicsState _physicsState;

    public Vector3 LinearVelocity
    {
        get { return _linearVelocity = PhysicsBody?.GetLinearVelocity() ?? _linearVelocity; }
        set
        {
            _linearVelocity = value;
            PhysicsBody?.SetLinearVelocity(value);
        }
    }

    public Vector3 AngularVelocity
    {
        get { return _angularVelocity = PhysicsBody?.GetAngularVelocity() ?? _angularVelocity; }
        set
        {
            _angularVelocity = value;
            PhysicsBody?.SetAngularVelocity(value);
        }
    }

    public PhysicsState PhysicsState
    {
        get { return _physicsState = PhysicsBody?.GetState() ?? _physicsState; }
        set
        {
            _physicsState = value;
            PhysicsBody?.SetState(value);
        }
    }

    public float Mass
    {
        get { return _mass = PhysicsBody?.GetMass() ?? _mass; }
        set
        {
            _mass = value;
            PhysicsBody?.SetMass(value);
        }
    }

    protected IPhysicsBody? PhysicsBody { get; set; }

    public void PrePhysicsUpdate()
    {
        if (PhysicsState != PhysicsState.Simulated)
        {
            Debug.Assert(PhysicsBody != null);
            var worldTransform = GetTransform(Space.World);
            PhysicsBody.SetPosition(worldTransform.Position);
            PhysicsBody.SetOrientation(worldTransform.Orientation);
        }
    }


    public abstract void ProcessHit(IPhysicsBody body, RayCastResult result);

    public override void Start()
    {
        PhysicsBody = CreateBody(GetTransform(Space.World), PhysicsState);
        PhysicsBody.SetLinearVelocity(_linearVelocity);
        PhysicsBody.SetAngularVelocity(_angularVelocity);
        PhysicsBody.SetMass(_mass);
        base.Start();
    }


    public override void Update(float deltaSeconds)
    {
        Debug.Assert(PhysicsBody != null);

        if (PhysicsState == PhysicsState.Simulated)
            SetTransform(
                new Transform
                {
                    Position = PhysicsBody.GetPosition(),
                    Orientation = PhysicsBody.GetOrientation(),
                    Scale = PhysicsBody.GetScale()
                },
                Space.World
            );

        base.Update(deltaSeconds);
    }

    protected abstract IPhysicsBody CreateBody(in Transform transform, in PhysicsState state);
}