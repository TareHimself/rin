using System.Diagnostics;
using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Math;
using Rin.World.Physics;

namespace Rin.World.Components;

public abstract class SingleBodyPhysicsComponent : WorldComponent
{
    private Vector3 _angularVelocity;
    private Vector3 _linearVelocity;
    private float _mass = 1.0f;
    private PhysicsState _physicsState;

    public Vector3 LinearVelocity
    {
        get { return _linearVelocity = PhysicsBody.IsValid ? Owner!.World!.PhysicsSystem.GetLinearVelocity(PhysicsBody) : _linearVelocity; }
        set
        {
            _linearVelocity = value;
            if (PhysicsBody.IsValid) Owner!.World!.PhysicsSystem.SetLinearVelocity(PhysicsBody, value);
        }
    }

    public Vector3 AngularVelocity
    {
        get { return _angularVelocity = PhysicsBody.IsValid ? Owner!.World!.PhysicsSystem.GetAngularVelocity(PhysicsBody) : _angularVelocity; }
        set
        {
            _angularVelocity = value;
            if (PhysicsBody.IsValid) Owner!.World!.PhysicsSystem.SetAngularVelocity(PhysicsBody, value);
        }
    }

    public PhysicsState PhysicsState
    {
        get { return _physicsState = PhysicsBody.IsValid ? Owner!.World!.PhysicsSystem.GetState(PhysicsBody) : _physicsState; }
        set
        {
            _physicsState = value;
            if (PhysicsBody.IsValid) Owner!.World!.PhysicsSystem.SetState(PhysicsBody, value);
        }
    }

    public float Mass
    {
        get { return _mass = PhysicsBody.IsValid ? Owner!.World!.PhysicsSystem.GetMass(PhysicsBody) : _mass; }
        set
        {
            _mass = value;
            if (PhysicsBody.IsValid) Owner!.World!.PhysicsSystem.SetMass(PhysicsBody, value);
        }
    }

    protected PhysicsBodyHandle PhysicsBody { get; set; } = PhysicsBodyHandle.Invalid;

    public override void PrePhysicsUpdate()
    {
        // Only kinematic (Controlled) bodies are driven from the component each step. Static bodies
        // are placed once at creation and must not be moved every frame.
        if (PhysicsState == PhysicsState.Controlled)
        {
            Debug.Assert(PhysicsBody.IsValid);
            var worldTransform = GetTransform(Space.World);
            Owner!.World!.PhysicsSystem.SetPosition(PhysicsBody, worldTransform.Position);
            Owner!.World!.PhysicsSystem.SetOrientation(PhysicsBody, worldTransform.Orientation);
        }
    }

    public override void Start()
    {
        PhysicsBody = CreateBody(GetTransform(Space.World), PhysicsState);
        Owner!.World!.RegisterPhysicsBody(PhysicsBody, this);
        Owner!.World!.PhysicsSystem.SetLinearVelocity(PhysicsBody, _linearVelocity);
        Owner!.World!.PhysicsSystem.SetAngularVelocity(PhysicsBody, _angularVelocity);
        Owner!.World!.PhysicsSystem.SetMass(PhysicsBody, _mass);
        base.Start();
    }

    public override void Update(float deltaSeconds)
    {
        Debug.Assert(PhysicsBody.IsValid);

        if (PhysicsState == PhysicsState.Simulated)
            SetTransform(
                new Transform
                {
                    Position = Owner!.World!.PhysicsSystem.GetPosition(PhysicsBody),
                    Orientation = Owner!.World!.PhysicsSystem.GetOrientation(PhysicsBody),
                    Scale = Owner!.World!.PhysicsSystem.GetScale(PhysicsBody)
                },
                Space.World
            );

        base.Update(deltaSeconds);
    }

    protected abstract PhysicsBodyHandle CreateBody(in Transform transform, in PhysicsState state);
}
