using System.Diagnostics;
using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Math;
using Rin.World.Physics;

namespace Rin.World.Components;

public abstract class SingleBodyPhysicsComponent : WorldComponent, IPhysicsContactReceiver
{
    private Vector3 _angularVelocity;
    private Vector3 _linearVelocity;
    private float _mass = 1.0f;
    private PhysicsState _physicsState;
    private bool _isTrigger;

    /// <summary>
    ///     Other bodies pass through a trigger and are reported through <see cref="OnOverlapBegin" /> and
    ///     <see cref="OnOverlapEnd" /> instead of colliding. Can be flipped at any time.
    /// </summary>
    public bool IsTrigger
    {
        get { return _isTrigger = PhysicsBody.IsValid ? Owner!.World!.PhysicsSystem.GetTrigger(PhysicsBody) : _isTrigger; }
        set
        {
            _isTrigger = value;
            if (PhysicsBody.IsValid) Owner!.World!.PhysicsSystem.SetTrigger(PhysicsBody, value);
        }
    }

    public event Action<SingleBodyPhysicsComponent, IWorldComponent?>? OnCollisionBegin;
    public event Action<SingleBodyPhysicsComponent, IWorldComponent?>? OnCollisionEnd;
    public event Action<SingleBodyPhysicsComponent, IWorldComponent?>? OnOverlapBegin;
    public event Action<SingleBodyPhysicsComponent, IWorldComponent?>? OnOverlapEnd;

    public void OnPhysicsContact(in PhysicsContactEvent contact, IWorldComponent? other)
    {
        var handler = (contact.IsTrigger, contact.Kind) switch
        {
            (true, PhysicsContactKind.Begin) => OnOverlapBegin,
            (true, PhysicsContactKind.End) => OnOverlapEnd,
            (false, PhysicsContactKind.Begin) => OnCollisionBegin,
            _ => OnCollisionEnd
        };
        handler?.Invoke(this, other);
    }

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
        Owner!.World!.PhysicsSystem.SetTrigger(PhysicsBody, _isTrigger);
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
