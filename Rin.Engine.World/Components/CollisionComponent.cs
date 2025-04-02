using System.Numerics;
using Rin.Engine.World.Physics;

namespace Rin.Engine.World.Components;

public abstract class CollisionComponent : SceneComponent, IPhysicsComponent
{
    private IPhysicsBody? _physicsBody;
    private bool _simulating;

    public Vector3 Velocity { get; set; }
    public Vector3 AngularVelocity { get; set; }
    public float Mass { get; set; } = 1.0f;

    public bool IsSimulating
    {
        get
        {
            if (_physicsBody is { } body)
            {
                _simulating = body.IsSimulating;
                return body.IsSimulating;
            }

            return _simulating;
        }
        set
        {
            if (_physicsBody is { } body)
            {
                body.SetSimulatePhysics(value);
                _simulating = body.IsSimulating;
            }
            else
            {
                _simulating = value;
            }
        }
    }

    public void ProcessHit(IPhysicsBody body, RayCastResult result)
    {
        OnHit?.Invoke(result);
    }

    public override void Start()
    {
        base.Start();
        _physicsBody = CreatePhysicsBody();
        _physicsBody.SetSimulatePhysics(_simulating);
        _simulating = _physicsBody.IsSimulating;
    }

    public override void Stop()
    {
        base.Stop();
        _physicsBody?.Dispose();
    }

    public event Action<RayCastResult>? OnHit;

    protected abstract IPhysicsBody CreatePhysicsBody();
}